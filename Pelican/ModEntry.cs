using Microsoft.Xna.Framework;
using Pelican.Advisors;
using Pelican.Config;
using Pelican.Data;
using Pelican.Hud;
using Pelican.Instrumentation;
using Pelican.Model;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Objects;

namespace Pelican;

/// <summary>
/// Pelican — assistente pessoal de Stardew Valley.
///
/// GARANTIA DE MULTIPLAYER (ver MULTIPLAYER.md na raiz do repo):
///   - SOMENTE LEITURA: nenhuma linha deste mod escreve estado de jogo.
///   - SOMENTE DESENHO: so RenderedHud.
///   - ZERO CONTEUDO: nao registra item, NPC, mapa nem receita. E adicionar
///     conteudo que quebra cliente vanilla; este mod nao adiciona nada.
///   - ZERO REDE: nao existe uma chamada de IMultiplayer.SendMessage aqui.
/// Por isso a namorada pode entrar pelo celular sem instalar nada.
/// </summary>
public sealed class ModEntry : Mod
{
    private ModConfig Config = null!;
    private HudRenderer Hud = null!;
    private UsageLog Usage = null!;

    private readonly List<IAdvisor> Advisors = new();
    private WeatherLuckAdvisor? WeatherAdvisor;
    private HeldItemAdvisor? HeldAdvisor;

    /// <summary>Ultimo slot da barra de ferramentas, para detectar troca de item na mao.</summary>
    private int LastToolIndex = -1;

    private GameSnapshot? Snapshot;
    private IReadOnlyList<Insight> Insights = Array.Empty<Insight>();
    private bool Dirty = true;

    /// <summary>Bundles ja anunciados como prontos, para nao repetir a notificacao.</summary>
    private readonly HashSet<string> AnnouncedReady = new();

    private Dictionary<string, RoomReward> RoomRewards = new();
    private List<Secret> Secrets = new();

    public override void Entry(IModHelper helper)
    {
        this.Config = helper.ReadConfig<ModConfig>();
        this.Hud = new HudRenderer(this.Config);
        this.Usage = new UsageLog(helper.DirectoryPath, this.Monitor, this.Config.RegistrarUso);

        this.LoadAssets(helper);
        this.RegisterAdvisors(helper);

        helper.Events.GameLoop.SaveLoaded += this.OnSaveLoaded;
        helper.Events.GameLoop.DayStarted += this.OnDayStarted;
        helper.Events.GameLoop.DayEnding += this.OnDayEnding;
        helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
        helper.Events.Player.InventoryChanged += this.OnInventoryChanged;
        helper.Events.World.ChestInventoryChanged += this.OnChestInventoryChanged;
        helper.Events.Display.RenderedHud += this.OnRenderedHud;
        helper.Events.Input.ButtonPressed += this.OnButtonPressed;

        helper.ConsoleCommands.Add("pelican_dump", "Despeja o estado que o Pelican esta lendo (diagnostico).", this.CommandDump);
        helper.ConsoleCommands.Add("pelican_uso", "Mostra o placar do Portao 1.", this.CommandUsage);

        this.Monitor.Log("Pelican carregado. Somente leitura, sem conteudo novo, sem rede.", LogLevel.Info);
    }

    // ------------------------------------------------------------------ setup

    private void LoadAssets(IModHelper helper)
    {
        try
        {
            var rooms = helper.Data.ReadJsonFile<RoomRewardFile>("assets/room-rewards.json");
            this.RoomRewards = rooms?.Salas ?? new Dictionary<string, RoomReward>();
        }
        catch (Exception ex)
        {
            this.Monitor.Log($"Nao consegui ler room-rewards.json: {ex.Message}", LogLevel.Warn);
        }

        try
        {
            var secrets = helper.Data.ReadJsonFile<SecretsFile>("assets/secrets.json");
            this.Secrets = secrets?.Segredos ?? new List<Secret>();
        }
        catch (Exception ex)
        {
            this.Monitor.Log($"Nao consegui ler secrets.json: {ex.Message}", LogLevel.Warn);
        }

        this.Monitor.Log($"Assets: {this.RoomRewards.Count} salas, {this.Secrets.Count} segredos.", LogLevel.Trace);
    }

    private void RegisterAdvisors(IModHelper helper)
    {
        if (this.Config.AvisarBundles)
            this.Advisors.Add(new BundleAdvisor());

        if (this.Config.AvisarBau)
            this.Advisors.Add(new ChestStagingAdvisor());

        if (this.Config.AvisarCalendario)
            this.Advisors.Add(new CalendarAdvisor(helper.GameContent, this.Monitor));

        if (this.Config.AvisarClimaSorte)
        {
            this.WeatherAdvisor = new WeatherLuckAdvisor(helper.GameContent, this.Monitor);
            this.Advisors.Add(this.WeatherAdvisor);
        }

        if (this.Config.AvisarPerdiveis)
            this.Advisors.Add(new MissableAdvisor(this.Secrets));

        if (this.Config.AvisarItemNaMao)
        {
            this.HeldAdvisor = new HeldItemAdvisor(helper.GameContent, this.Monitor);
            this.Advisors.Add(this.HeldAdvisor);
        }

        if (this.Config.AvisarColheita)
            this.Advisors.Add(new HarvestAdvisor());
    }

    // ----------------------------------------------------------------- eventos

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        this.Dirty = true;
        this.AnnouncedReady.Clear();
    }

    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        this.WeatherAdvisor?.InvalidateCache();
        this.HeldAdvisor?.InvalidateCache();
        this.AnnouncedReady.Clear();
        this.Dirty = true;

        this.Refresh();

        if (this.Snapshot != null)
            this.Usage.StartDay(this.Snapshot, this.Insights);
    }

    private void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        this.Usage.EndDay();
    }

    /// <summary>
    /// Trocar de item na barra de ferramentas muda o que o HUD deve dizer, mas nao
    /// existe evento para isso. Verifica a cada quarto de segundo, que e barato e
    /// imperceptivelmente rapido.
    /// </summary>
    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!e.IsMultipleOf(15) || !Context.IsWorldReady)
            return;

        int current = Game1.player?.CurrentToolIndex ?? -1;
        if (current == this.LastToolIndex)
            return;

        this.LastToolIndex = current;
        this.Dirty = true;
    }

    private void OnInventoryChanged(object? sender, InventoryChangedEventArgs e)
    {
        if (!e.IsLocalPlayer)
            return;

        foreach (Item item in e.Added)
            this.Usage.NoteItemSeen(item?.QualifiedItemId ?? "");

        this.Dirty = true;
    }

    /// <summary>
    /// O evento que resolve o pedido original: detectar quando um item vai para o bau.
    /// Se o bau marcado passou a conter tudo que falta para um bundle, avisa na hora.
    /// </summary>
    private void OnChestInventoryChanged(object? sender, ChestInventoryChangedEventArgs e)
    {
        if (!this.Config.TemBauMarcado)
            return;

        Chest? staging = SnapshotBuilder.FindStagingChest(this.Config);
        if (staging is null || !ReferenceEquals(staging, e.Chest))
            return;

        foreach (Item item in e.Added)
            this.Usage.NoteItemSeen(item?.QualifiedItemId ?? "");

        this.Dirty = true;
        this.Refresh();

        this.AnnounceReadyBundles();
    }

    private void AnnounceReadyBundles()
    {
        if (this.Snapshot is null)
            return;

        foreach (BundleInfo bundle in ChestStagingAdvisor.BundlesReadyInChest(this.Snapshot))
        {
            if (!this.AnnouncedReady.Add(bundle.Key))
                continue;

            string name = !string.IsNullOrWhiteSpace(bundle.DisplayName) ? bundle.DisplayName : bundle.Name;
            RoomReward? reward = this.Snapshot.RewardFor(bundle.Room);

            string message = reward is not null && this.Snapshot.WouldCompleteRoom(bundle)
                ? $"Bau: {name} completo — fecha {reward.Nome} e libera {reward.Destrava}"
                : $"Bau: {name} completo — so levar ao Centro Comunitario";

            Game1.addHUDMessage(new HUDMessage(message, HUDMessage.newQuest_type));
        }
    }

    private void OnRenderedHud(object? sender, RenderedHudEventArgs e)
    {
        if (!HudRenderer.CanDraw())
            return;

        try
        {
            this.Refresh();

            if (this.Snapshot != null)
                this.Hud.Draw(e.SpriteBatch, this.Snapshot, this.Insights);
        }
        catch (Exception ex)
        {
            // Um erro de desenho nunca pode derrubar o jogo, muito menos numa
            // sessao de multiplayer. Desliga o HUD e segue.
            this.Monitor.LogOnce($"Erro ao desenhar o HUD, desligando o painel: {ex.Message}", LogLevel.Error);
            this.Config.MostrarHudCompacto = false;
            this.Hud.PanelOpen = false;
        }
    }

    private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsPlayerFree)
            return;

        if (e.Button == this.Config.TeclaPainel)
        {
            this.Dirty = true;
            this.Hud.PanelOpen = !this.Hud.PanelOpen;
        }
        else if (e.Button == this.Config.TeclaMarcarBau)
        {
            this.MarkChestUnderCursor();
        }
    }

    /// <summary>Marca o bau sob o cursor como bau do Centro Comunitario.</summary>
    private void MarkChestUnderCursor()
    {
        try
        {
            GameLocation? location = Game1.currentLocation;
            if (location is null)
                return;

            Vector2 tile = Game1.currentCursorTile;

            if (!location.objects.TryGetValue(tile, out StardewValley.Object? obj) || obj is not Chest)
            {
                Game1.addHUDMessage(new HUDMessage("Pelican: aponte para um bau e aperte de novo.", HUDMessage.error_type));
                return;
            }

            this.Config.BauLocal = location.NameOrUniqueName;
            this.Config.BauX = (int)tile.X;
            this.Config.BauY = (int)tile.Y;
            this.Helper.WriteConfig(this.Config);

            this.AnnouncedReady.Clear();
            this.Dirty = true;

            Game1.addHUDMessage(new HUDMessage("Pelican: bau do Centro Comunitario marcado.", HUDMessage.newQuest_type));
        }
        catch (Exception ex)
        {
            this.Monitor.Log($"Nao consegui marcar o bau: {ex.Message}", LogLevel.Warn);
        }
    }

    // ------------------------------------------------------------------ nucleo

    /// <summary>Recalcula o snapshot e os avisos, se algo mudou.</summary>
    private void Refresh()
    {
        if (!this.Dirty || !Context.IsWorldReady)
            return;

        this.Dirty = false;

        try
        {
            this.Snapshot = SnapshotBuilder.Build(this.Config, this.RoomRewards, this.Monitor);
            this.Insights = this.RunAdvisors(this.Snapshot);
        }
        catch (Exception ex)
        {
            this.Monitor.Log($"Falha ao montar o estado: {ex.Message}", LogLevel.Warn);
            this.Insights = Array.Empty<Insight>();
        }
    }

    /// <summary>
    /// Roda cada advisor isolado. Um advisor que quebra nao pode levar os outros
    /// junto, senao um bug de calendario apaga os avisos de bundle.
    /// </summary>
    private IReadOnlyList<Insight> RunAdvisors(GameSnapshot snapshot)
    {
        var all = new List<Insight>();

        foreach (IAdvisor advisor in this.Advisors)
        {
            try
            {
                all.AddRange(advisor.Advise(snapshot));
            }
            catch (Exception ex)
            {
                this.Monitor.LogOnce($"Advisor '{advisor.Name}' falhou: {ex.Message}", LogLevel.Warn);
            }
        }

        return all
            .GroupBy(static i => i.Id)
            .Select(static g => g.First())
            .OrderByDescending(static i => i.Urgency)
            .ToList();
    }

    // ---------------------------------------------------------------- comandos

    private void CommandDump(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            this.Monitor.Log("Carregue um save primeiro.", LogLevel.Info);
            return;
        }

        this.Dirty = true;
        this.Refresh();

        if (this.Snapshot is null)
        {
            this.Monitor.Log("Sem estado. Veja os avisos acima.", LogLevel.Warn);
            return;
        }

        GameSnapshot s = this.Snapshot;
        var lines = new List<string>
        {
            "",
            $"=== PELICAN — {s.DateLabel} ===",
            $"Clima hoje: {s.WeatherToday} · amanha: {s.WeatherTomorrow} · {s.LuckLabel} ({s.DailyLuck:F3})",
            $"Bundles lidos: {s.Bundles.Count} · incompletos: {s.IncompleteBundles.Count()}",
            $"Inventario: {s.Inventory.Count} itens · bau marcado: {(s.HasStagingChest ? $"{this.Config.BauLocal} ({this.Config.BauX},{this.Config.BauY}) com {s.StagedItems.Count} itens" : "nenhum")}",
            $"Advisors ativos: {string.Join(", ", this.Advisors.Select(static a => a.Name))}",
            $"Fazendeiros: {string.Join(", ", s.Farmers.Select(static f => f.Name))}",
            ""
        };

        if (s.Bundles.Count == 0)
        {
            lines.Add("!! Nenhum bundle lido. Se o Centro Comunitario ainda existe neste save,");
            lines.Add("!! o parser precisa de ajuste — mande esta saida junto do log do SMAPI.");
        }
        else
        {
            lines.Add("--- Bundles incompletos ---");
            foreach (BundleInfo b in s.IncompleteBundles.Take(20))
            {
                lines.Add($"  [{b.Room}] {b.DisplayName} {b.ProgressBar()} ({b.CompletedCount}/{b.SlotsRequired})");
                lines.Add($"      falta: {string.Join(", ", b.MissingSlots.Select(static x => x.ToString()))}");
                lines.Add($"      recompensa: {b.RewardLabel}");
            }
        }

        lines.Add("");
        lines.Add($"--- Avisos gerados ({this.Insights.Count}) ---");
        foreach (Insight i in this.Insights.Take(25))
            lines.Add($"  [{i.Urgency}] ({i.Source}) {i.Title} | {i.Consequence}");

        this.Monitor.Log(string.Join(Environment.NewLine, lines), LogLevel.Info);
    }

    private void CommandUsage(string command, string[] args)
    {
        this.Monitor.Log(Environment.NewLine + this.Usage.Summary(), LogLevel.Info);
    }
}
