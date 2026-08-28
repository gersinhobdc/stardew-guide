using System.Text.Json;
using Pelican.Model;
using StardewModdingAPI;

namespace Pelican.Instrumentation;

/// <summary>
/// Mede se o Pelican vale a pena, sem depender de voce manter um diario.
///
/// O plano v3 apostava a vida do projeto num USO.md preenchido a mao por 14 dias.
/// Isso e o componente menos confiavel de qualquer plano pessoal. Aqui o proprio
/// mod registra: quais avisos apareceram e se voce agiu neles.
///
/// Portao 1: depois de 14 dias jogados, se "agidos" nao passar de 10, o projeto
/// para. Rode "pelican_uso" no console do SMAPI para ver o placar.
/// </summary>
public sealed class UsageLog
{
    private readonly string FilePath;
    private readonly IMonitor Monitor;
    private readonly bool Enabled;

    private DayRecord? Current;

    public UsageLog(string modDirectory, IMonitor monitor, bool enabled)
    {
        this.FilePath = Path.Combine(modDirectory, "usage.jsonl");
        this.Monitor = monitor;
        this.Enabled = enabled;
    }

    private sealed class DayRecord
    {
        public string Data { get; set; } = "";
        public int Mostrados { get; set; }
        public HashSet<string> Ids { get; set; } = new();

        /// <summary>Item esperado -> id do aviso que o mencionou.</summary>
        public Dictionary<string, string> Esperados { get; set; } = new();

        public HashSet<string> Agidos { get; set; } = new();
    }

    /// <summary>Abre o registro do dia com os avisos que foram exibidos.</summary>
    public void StartDay(GameSnapshot snapshot, IReadOnlyList<Insight> insights)
    {
        if (!this.Enabled)
            return;

        var record = new DayRecord
        {
            Data = $"A{snapshot.Year}-{snapshot.Season}-{snapshot.DayOfMonth:00}",
            Mostrados = insights.Count,
            Ids = insights.Select(static i => i.Id).ToHashSet()
        };

        foreach (Insight insight in insights)
        {
            foreach (string itemId in insight.Items)
                record.Esperados[itemId] = insight.Id;
        }

        this.Current = record;
    }

    /// <summary>
    /// Um item entrou no inventario ou no bau. Se algum aviso de hoje mencionava
    /// esse item, conta como "voce agiu por causa do aviso".
    /// </summary>
    public void NoteItemSeen(string itemId)
    {
        if (!this.Enabled || this.Current is null || string.IsNullOrWhiteSpace(itemId))
            return;

        // Ids de bundle vem sem qualificador; normaliza os dois lados.
        string bare = itemId.Contains(')') ? itemId[(itemId.IndexOf(')') + 1)..] : itemId;

        if (this.Current.Esperados.TryGetValue(bare, out string? insightId))
            this.Current.Agidos.Add(insightId);
    }

    /// <summary>Fecha o dia e grava a linha.</summary>
    public void EndDay()
    {
        if (!this.Enabled || this.Current is null)
            return;

        try
        {
            var line = new
            {
                data = this.Current.Data,
                mostrados = this.Current.Mostrados,
                agidos = this.Current.Agidos.Count,
                ids_agidos = this.Current.Agidos.ToArray()
            };

            File.AppendAllText(this.FilePath, JsonSerializer.Serialize(line) + Environment.NewLine);
        }
        catch (Exception ex)
        {
            this.Monitor.Log($"Nao consegui gravar usage.jsonl: {ex.Message}", LogLevel.Trace);
        }
        finally
        {
            this.Current = null;
        }
    }

    /// <summary>Placar do Portao 1, para o comando de console.</summary>
    public string Summary()
    {
        if (!File.Exists(this.FilePath))
            return "Ainda nao ha registro de uso. Jogue alguns dias com o mod ativo.";

        try
        {
            string[] lines = File.ReadAllLines(this.FilePath);
            int days = lines.Length;
            int acted = 0;
            int shown = 0;

            foreach (string line in lines)
            {
                using JsonDocument doc = JsonDocument.Parse(line);
                JsonElement root = doc.RootElement;

                if (root.TryGetProperty("agidos", out JsonElement a))
                    acted += a.GetInt32();

                if (root.TryGetProperty("mostrados", out JsonElement m))
                    shown += m.GetInt32();
            }

            string verdict = days < 14
                ? $"Faltam {14 - days} dias para o Portao 1 poder ser julgado."
                : acted >= 10
                    ? "PORTAO 1 PASSOU: o mod esta mudando decisoes suas. Siga."
                    : "PORTAO 1 REPROVOU: encerre o desenvolvimento. Voce fica com os mods prontos, que ja valem.";

            return $"Dias registrados: {days} · avisos mostrados: {shown} · avisos que voce agiu: {acted}\n{verdict}";
        }
        catch (Exception ex)
        {
            return $"Nao consegui ler usage.jsonl: {ex.Message}";
        }
    }
}
