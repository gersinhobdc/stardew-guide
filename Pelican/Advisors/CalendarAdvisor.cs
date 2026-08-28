using Pelican.Data;
using Pelican.Model;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Crops;

namespace Pelican.Advisors;

/// <summary>
/// O que acaba, o que vence e o que acontece nos proximos dias.
///
/// Tudo vem de Data/Crops, Data/Characters e Data/Festivals - nenhum dado curado.
/// E por isso que a 1.7 nao invalida este advisor: ela muda os dados, ele le os novos.
/// </summary>
public sealed class CalendarAdvisor : IAdvisor
{
    private readonly IGameContentHelper Content;
    private readonly IMonitor Monitor;

    public CalendarAdvisor(IGameContentHelper content, IMonitor monitor)
    {
        this.Content = content;
        this.Monitor = monitor;
    }

    public string Name => "Calendario";

    public IEnumerable<Insight> Advise(GameSnapshot snapshot)
    {
        var insights = new List<Insight>();

        insights.AddRange(this.SeasonEnding(snapshot));
        insights.AddRange(this.PlantingDeadlines(snapshot));
        insights.AddRange(this.Birthdays(snapshot));
        insights.AddRange(this.Festivals(snapshot));

        return insights;
    }

    /// <summary>Aviso de fim de estacao, so na ultima semana.</summary>
    private IEnumerable<Insight> SeasonEnding(GameSnapshot snapshot)
    {
        if (!snapshot.IsLastWeekOfSeason)
            yield break;

        int left = snapshot.DaysLeftInSeason;

        yield return new Insight(
            Id: $"estacao-acabando:{snapshot.Season}:{snapshot.Year}",
            Urgency: left <= 2 ? Urgency.Today : Urgency.Soon,
            Title: left == 1
                ? $"ULTIMO DIA de {snapshot.SeasonPt}"
                : $"Faltam {left} dias para acabar {snapshot.SeasonPt}",
            Detail: "Culturas nao colhidas morrem na virada. Peixes e forragens da estacao somem.",
            Consequence: "o que for da estacao so volta no ano que vem",
            Source: "Calendario"
        );
    }

    /// <summary>
    /// Ultimo dia util para plantar cada cultura. Calculado de Data/Crops:
    /// soma dos dias de fase contra os dias que restam na estacao.
    /// </summary>
    private IEnumerable<Insight> PlantingDeadlines(GameSnapshot snapshot)
    {
        var results = new List<Insight>();

        try
        {
            var crops = this.Content.Load<Dictionary<string, CropData>>("Data/Crops");
            if (!TryParseSeason(snapshot.Season, out Season season))
                return results;

            // Dias disponiveis para crescer se plantar hoje (planta hoje, colhe ate o dia 28).
            int daysAvailable = snapshot.DaysLeftInSeason - 1;

            foreach ((string seedId, CropData? data) in crops)
            {
                if (data?.Seasons is null || !data.Seasons.Contains(season))
                    continue;

                int growth = data.DaysInPhase?.Sum() ?? 0;
                if (growth <= 0)
                    continue;

                // So interessa quando o prazo e HOJE ou amanha. Antes disso e ruido.
                int slack = daysAvailable - growth;
                if (slack is < 0 or > 1)
                    continue;

                string cropName = ItemNames.Resolve(data.HarvestItemId ?? seedId);
                string seedName = ItemNames.Resolve(seedId);

                results.Add(new Insight(
                    Id: $"plantio:{seedId}:{snapshot.Season}:{snapshot.Year}",
                    Urgency: slack == 0 ? Urgency.Today : Urgency.Soon,
                    Title: slack == 0
                        ? $"HOJE e o ultimo dia para plantar {seedName}"
                        : $"Amanha e o ultimo dia para plantar {seedName}",
                    Detail: $"{cropName} leva {growth} dias e restam {daysAvailable} de cultivo nesta estacao.",
                    Consequence: "depois disso nao colhe antes da virada",
                    Source: "Calendario",
                    RelatedItemIds: new[] { seedId }
                ));
            }
        }
        catch (Exception ex)
        {
            this.Monitor.Log($"Nao consegui ler Data/Crops: {ex.Message}", LogLevel.Trace);
        }

        return results;
    }

    /// <summary>Aniversarios de hoje e dos proximos 2 dias, de Data/Characters.</summary>
    private IEnumerable<Insight> Birthdays(GameSnapshot snapshot)
    {
        var results = new List<Insight>();

        try
        {
            if (!TryParseSeason(snapshot.Season, out Season season))
                return results;

            foreach ((string name, var data) in Game1.characterData)
            {
                if (data is null || data.BirthSeason != season)
                    continue;

                int delta = data.BirthDay - snapshot.DayOfMonth;
                if (delta is < 0 or > 2)
                    continue;

                string displayName = SafeDisplayName(name);

                results.Add(new Insight(
                    Id: $"aniversario:{name}:{snapshot.Year}",
                    Urgency: delta == 0 ? Urgency.Today : Urgency.Soon,
                    Title: delta switch
                    {
                        0 => $"HOJE e aniversario de {displayName}",
                        1 => $"Amanha e aniversario de {displayName}",
                        _ => $"Aniversario de {displayName} em {delta} dias"
                    },
                    Detail: "Presente amado no aniversario vale 8x amizade. Use F1 no NPC para ver o que ele gosta.",
                    Consequence: delta == 0 ? "so hoje vale o bonus" : "",
                    Source: "Calendario"
                ));
            }
        }
        catch (Exception ex)
        {
            this.Monitor.Log($"Nao consegui ler aniversarios: {ex.Message}", LogLevel.Trace);
        }

        return results;
    }

    /// <summary>Festivais nos proximos 7 dias, de Data/Festivals/FestivalDates.</summary>
    private IEnumerable<Insight> Festivals(GameSnapshot snapshot)
    {
        var results = new List<Insight>();

        try
        {
            var dates = this.Content.Load<Dictionary<string, string>>("Data/Festivals/FestivalDates");

            for (int delta = 0; delta <= 7; delta++)
            {
                int day = snapshot.DayOfMonth + delta;
                if (day > 28)
                    break;

                string key = $"{snapshot.Season}{day}";
                if (!dates.TryGetValue(key, out string? festivalName))
                    continue;

                results.Add(new Insight(
                    Id: $"festival:{key}:{snapshot.Year}",
                    Urgency: delta == 0 ? Urgency.Today : Urgency.Soon,
                    Title: delta switch
                    {
                        0 => $"HOJE tem {festivalName}",
                        1 => $"Amanha tem {festivalName}",
                        _ => $"{festivalName} em {delta} dias"
                    },
                    Detail: delta == 0
                        ? "Durante o festival a fazenda fica parada; planeje o dia."
                        : "Festival ocupa o dia inteiro. Nao deixe cultura para colher nesse dia.",
                    Consequence: "",
                    Source: "Calendario"
                ));
            }
        }
        catch (Exception ex)
        {
            this.Monitor.Log($"Nao consegui ler festivais: {ex.Message}", LogLevel.Trace);
        }

        return results;
    }

    private static string SafeDisplayName(string internalName)
    {
        try
        {
            NPC? npc = Game1.getCharacterFromName(internalName);
            if (npc != null && !string.IsNullOrWhiteSpace(npc.displayName))
                return npc.displayName;
        }
        catch
        {
            // NPC ainda nao carregado. O nome interno serve.
        }

        return internalName;
    }

    private static bool TryParseSeason(string season, out Season parsed)
    {
        switch (season?.ToLowerInvariant())
        {
            case "spring": parsed = Season.Spring; return true;
            case "summer": parsed = Season.Summer; return true;
            case "fall": parsed = Season.Fall; return true;
            case "winter": parsed = Season.Winter; return true;
            default: parsed = Season.Spring; return false;
        }
    }
}
