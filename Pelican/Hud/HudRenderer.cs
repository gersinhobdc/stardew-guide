using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pelican.Config;
using Pelican.Model;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace Pelican.Hud;

/// <summary>
/// Desenha. So isso.
///
/// O HUD nao sabe o que e bundle, bau ou segredo - ele so sabe renderizar Insight
/// ordenado por urgencia. E por isso que adicionar capacidade nunca exige mexer aqui.
/// </summary>
public sealed class HudRenderer
{
    private const int Padding = 16;
    private const int LineSpacing = 4;

    private readonly ModConfig Config;

    public HudRenderer(ModConfig config)
    {
        this.Config = config;
    }

    /// <summary>Painel completo aberto? Alternado pela tecla do painel.</summary>
    public bool PanelOpen { get; set; }

    /// <summary>Painel compacto escondido pela tecla de esconder (nao persiste na config).</summary>
    public bool Hidden { get; set; }

    private static Color ColorFor(Urgency urgency) => urgency switch
    {
        Urgency.Missable => new Color(200, 40, 40),
        Urgency.Today => new Color(190, 100, 20),
        Urgency.Soon => Game1.textColor,
        _ => new Color(110, 110, 110)
    };

    private static string PrefixFor(Urgency urgency) => urgency switch
    {
        Urgency.Missable => "[!]",
        Urgency.Today => "[hoje]",
        Urgency.Soon => "[logo]",
        _ => "-"
    };

    /// <summary>Deve desenhar agora? Fora de menu, fora de cutscene.</summary>
    public static bool CanDraw()
    {
        return Context.IsWorldReady
            && Game1.activeClickableMenu is null
            && !Game1.eventUp
            && !Game1.isFestival()
            && Game1.currentMinigame is null;
    }

    public void Draw(SpriteBatch batch, GameSnapshot snapshot, IReadOnlyList<Insight> insights)
    {
        if (this.PanelOpen)
            this.DrawFullPanel(batch, snapshot, insights);
        else if (this.Config.MostrarHudCompacto && !this.Hidden)
            this.DrawCompact(batch, snapshot, insights);
    }

    /// <summary>Corta a linha no limite configurado, para o HUD nunca tomar a tela.</summary>
    private string Truncate(string text)
    {
        int max = Math.Max(20, this.Config.MaxCaracteresPorLinha);
        return text.Length <= max ? text : text[..(max - 1)] + "…";
    }

    // ---------------------------------------------------------------- compacto

    private void DrawCompact(SpriteBatch batch, GameSnapshot snapshot, IReadOnlyList<Insight> insights)
    {
        // Regra do painel compacto: o que tem prazo, mais o que e contextual.
        //
        // Filtrar so por urgencia esconderia justamente os avisos de item na mao,
        // que nascem como Info — eles nao sao urgentes, sao IMEDIATOS, e somem
        // sozinhos quando voce troca de item. Contextual vem primeiro porque
        // responde ao que voce esta fazendo neste segundo.
        var contextual = insights
            .Where(static i => i.Contextual)
            .OrderByDescending(static i => i.Urgency)
            .ToList();

        var scheduled = insights
            .Where(static i => !i.Contextual && i.Urgency > Urgency.Info)
            .OrderByDescending(static i => i.Urgency)
            .ToList();

        var lines = contextual
            .Concat(scheduled)
            .Take(Math.Max(1, this.Config.MaxLinhasHud + contextual.Count))
            .ToList();

        var rows = new List<(string Text, Color Color)>();

        if (this.Config.MostrarLinhaDeStatus)
            rows.Add((this.Truncate(StatusLine(snapshot)), new Color(90, 90, 90)));

        rows.AddRange(lines.Select(i =>
        {
            string body = this.Config.HudCompactoSoTitulo ? i.Title : i.ToCompactLine();
            return (this.Truncate($"{PrefixFor(i.Urgency)} {body}"), ColorFor(i.Urgency));
        }));

        if (rows.Count == 0)
            return;

        SpriteFont font = Game1.smallFont;
        int width = (int)rows.Max(r => font.MeasureString(r.Text).X) + (Padding * 2);
        int lineHeight = (int)font.MeasureString("A").Y + LineSpacing;
        int height = (lineHeight * rows.Count) + (Padding * 2);

        (int x, int y) = this.AnchorFor(width, height);

        DrawBox(batch, x, y, width, height);

        for (int i = 0; i < rows.Count; i++)
        {
            Utility.drawTextWithShadow(
                batch,
                rows[i].Text,
                font,
                new Vector2(x + Padding, y + Padding + (i * lineHeight)),
                rows[i].Color
            );
        }
    }

    /// <summary>Data, clima de hoje e de amanha, e sorte — numa linha só.</summary>
    private static string StatusLine(GameSnapshot snapshot)
    {
        return $"{snapshot.DateLabel} · hoje {snapshot.WeatherToday} · amanha {snapshot.WeatherTomorrow} · {snapshot.LuckLabel}";
    }

    private (int, int) AnchorFor(int width, int height)
    {
        int margin = 24;
        int screenW = Game1.uiViewport.Width;
        int screenH = Game1.uiViewport.Height;

        return this.Config.CantoHud?.ToLowerInvariant() switch
        {
            "topright" => (screenW - width - margin, margin + 100),
            "bottomleft" => (margin, screenH - height - margin),
            "bottomright" => (screenW - width - margin, screenH - height - margin),
            _ => (margin, margin + 100)
        };
    }

    // ------------------------------------------------------------------- painel

    /// <summary>
    /// Painel completo. Este e o quadro que voce printa e manda pra ela:
    /// estado da fazenda, quem pode fazer o que, e o que acaba nesta estacao.
    /// </summary>
    private void DrawFullPanel(SpriteBatch batch, GameSnapshot snapshot, IReadOnlyList<Insight> insights)
    {
        SpriteFont font = Game1.smallFont;
        int lineHeight = (int)font.MeasureString("A").Y + LineSpacing;

        var rows = new List<(string Text, Color Color)>
        {
            ($"PELICAN — {snapshot.DateLabel} · {snapshot.WeatherToday} · {snapshot.LuckLabel}", Game1.textColor),
            ("", Game1.textColor)
        };

        // --- Falta para a fazenda ---
        var incomplete = snapshot.IncompleteBundles.ToList();
        if (incomplete.Count > 0)
        {
            rows.Add(("FALTA PARA A FAZENDA", new Color(60, 60, 130)));

            foreach (BundleInfo bundle in incomplete.OrderBy(static b => b.Remaining).Take(6))
            {
                string name = !string.IsNullOrWhiteSpace(bundle.DisplayName) ? bundle.DisplayName : bundle.Name;
                string missing = string.Join(", ", bundle.MissingSlots.Take(4).Select(static s => s.ToString()));
                rows.Add(($"  {bundle.ProgressBar()} {name}: {missing}", Game1.textColor));
            }

            rows.Add(("", Game1.textColor));
        }

        // --- Quem pode fazer o que ---
        if (snapshot.Farmers.Count > 1)
        {
            rows.Add(("QUEM PODE FAZER O QUE", new Color(60, 60, 130)));

            foreach (Farmer farmer in snapshot.Farmers)
            {
                rows.Add(($"  {DescribeFarmer(farmer)}", Game1.textColor));
            }

            rows.Add(("", Game1.textColor));
        }

        // --- Avisos ---
        var relevant = insights.OrderByDescending(static i => i.Urgency).Take(12).ToList();
        if (relevant.Count > 0)
        {
            rows.Add(("AVISOS", new Color(60, 60, 130)));

            foreach (Insight insight in relevant)
            {
                rows.Add(($"  {PrefixFor(insight.Urgency)} {insight.ToCompactLine()}", ColorFor(insight.Urgency)));
            }
        }

        rows.Add(("", Game1.textColor));
        rows.Add(("F9 fecha · F8 esconde o painel do canto · F10 marca o bau sob o cursor",
            new Color(110, 110, 110)));

        int width = (int)rows.Max(r => font.MeasureString(r.Text).X) + (Padding * 2);
        width = Math.Min(width, Game1.uiViewport.Width - 80);
        int height = (rows.Count * lineHeight) + (Padding * 2);

        int x = (Game1.uiViewport.Width - width) / 2;
        int y = Math.Max(40, (Game1.uiViewport.Height - height) / 2);

        DrawBox(batch, x, y, width, height);

        for (int i = 0; i < rows.Count; i++)
        {
            if (string.IsNullOrEmpty(rows[i].Text))
                continue;

            Utility.drawTextWithShadow(
                batch,
                rows[i].Text,
                font,
                new Vector2(x + Padding, y + Padding + (i * lineHeight)),
                rows[i].Color
            );
        }
    }

    /// <summary>"Gerson (Pesca 8 · Mineracao 5)" — base do "quem tem mais chance".</summary>
    private static string DescribeFarmer(Farmer farmer)
    {
        try
        {
            string name = string.IsNullOrWhiteSpace(farmer.Name) ? "?" : farmer.Name;
            return $"{name} — Pesca {farmer.FishingLevel} · Mineracao {farmer.MiningLevel} · "
                + $"Agricultura {farmer.FarmingLevel} · Coleta {farmer.ForagingLevel} · Combate {farmer.CombatLevel}";
        }
        catch
        {
            return "?";
        }
    }

    private static void DrawBox(SpriteBatch batch, int x, int y, int width, int height)
    {
        IClickableMenu.drawTextureBox(
            batch,
            Game1.menuTexture,
            new Rectangle(0, 256, 60, 60),
            x - Padding / 2,
            y - Padding / 2,
            width + Padding,
            height + Padding,
            Color.White,
            1f,
            false
        );
    }
}
