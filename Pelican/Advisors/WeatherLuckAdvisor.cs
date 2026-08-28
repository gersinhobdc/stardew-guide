using Pelican.Data;
using Pelican.Model;
using StardewModdingAPI;

namespace Pelican.Advisors;

/// <summary>
/// Cruza o clima e a sorte de HOJE com o que ainda falta.
///
/// Nao repete o icone de sorte do UI Info Suite 2. A diferenca e o cruzamento:
/// nao "hoje chove", mas "hoje chove E voce precisa de Enguia, que so aparece na chuva".
/// </summary>
public sealed class WeatherLuckAdvisor : IAdvisor
{
    private readonly IGameContentHelper Content;
    private readonly IMonitor Monitor;
    private IReadOnlyDictionary<string, FishInfo>? FishCache;

    public WeatherLuckAdvisor(IGameContentHelper content, IMonitor monitor)
    {
        this.Content = content;
        this.Monitor = monitor;
    }

    public string Name => "ClimaSorte";

    /// <summary>Chamado na virada do dia: os dados de peixe nao mudam durante a partida.</summary>
    public void InvalidateCache() => this.FishCache = null;

    public IEnumerable<Insight> Advise(GameSnapshot snapshot)
    {
        var insights = new List<Insight>();

        insights.AddRange(this.RainFishStillNeeded(snapshot));
        insights.AddRange(LuckOfTheDay(snapshot));

        return insights;
    }

    /// <summary>
    /// Chove hoje e algum bundle ainda pede um peixe que so aparece na chuva?
    /// Esse cruzamento e a razao deste advisor existir.
    /// </summary>
    private IEnumerable<Insight> RainFishStillNeeded(GameSnapshot snapshot)
    {
        var results = new List<Insight>();

        if (!snapshot.IsRaining)
            return results;

        this.FishCache ??= FishReader.ReadAll(this.Content, this.Monitor);
        if (this.FishCache.Count == 0)
            return results;

        // Ids de peixe que algum bundle incompleto ainda pede.
        var wanted = snapshot.IncompleteBundles
            .SelectMany(static b => b.MissingSlots)
            .Where(static s => !s.IsCategory)
            .Select(static s => s.ItemId)
            .Distinct()
            .ToList();

        foreach (string itemId in wanted)
        {
            if (!this.FishCache.TryGetValue(itemId, out FishInfo? fish) || !fish.NeedsRain)
                continue;

            string window = string.IsNullOrWhiteSpace(fish.TimeLabel) ? "" : $" ({fish.TimeLabel})";

            results.Add(new Insight(
                Id: $"peixe-chuva:{itemId}:{snapshot.Season}:{snapshot.DayOfMonth}",
                Urgency: Urgency.Today,
                Title: $"Chove hoje: {fish.Name} esta pegavel{window}",
                Detail: "Este peixe so aparece na chuva e ainda falta para um bundle. F1 nele mostra onde pescar.",
                Consequence: "so em dia de chuva",
                Source: "ClimaSorte",
                RelatedItemIds: new[] { itemId }
            ));
        }

        return results;
    }

    /// <summary>Sorte do dia, so quando ela e extrema o bastante para mudar a decisao.</summary>
    private static IEnumerable<Insight> LuckOfTheDay(GameSnapshot snapshot)
    {
        if (snapshot.DailyLuck >= 0.07)
        {
            yield return new Insight(
                Id: $"sorte-alta:{snapshot.Season}:{snapshot.DayOfMonth}:{snapshot.Year}",
                Urgency: Urgency.Today,
                Title: $"Dia de {snapshot.LuckLabel}: bom para Skull Cavern / minas",
                Detail: "Sorte alta aumenta escadas, geodos e drops raros. Se for descer, e hoje.",
                Consequence: "a sorte zera amanha",
                Source: "ClimaSorte"
            );
        }
        else if (snapshot.DailyLuck <= -0.07)
        {
            yield return new Insight(
                Id: $"sorte-baixa:{snapshot.Season}:{snapshot.DayOfMonth}:{snapshot.Year}",
                Urgency: Urgency.Info,
                Title: $"Dia de {snapshot.LuckLabel}: evite mina profunda",
                Detail: "Menos escadas e menos drop raro. Dia melhor para fazenda, social ou artesanato.",
                Consequence: "",
                Source: "ClimaSorte"
            );
        }
    }
}
