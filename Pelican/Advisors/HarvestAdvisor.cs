using Pelican.Model;

namespace Pelican.Advisors;

/// <summary>
/// Plantacoes.
///
/// O aviso que justifica este advisor nao e "faltam N dias" - a propria planta
/// mostra isso. E "estas N culturas NAO vao amadurecer antes da virada": semente
/// e tempo ja gastos que vao morrer com a estacao, e que nada no jogo avisa.
/// Quanto mais cedo voce sabe, mais cedo replanta com algo que da tempo.
/// </summary>
public sealed class HarvestAdvisor : IAdvisor
{
    public string Name => "Colheita";

    public IEnumerable<Insight> Advise(GameSnapshot snapshot)
    {
        CropSummary crops = snapshot.Crops;

        if (crops.WontMatureTotal > 0)
        {
            var detail = crops.WontMature
                .OrderByDescending(static p => p.Value)
                .Select(static p => $"{p.Value}x {p.Key}")
                .Take(5);

            yield return new Insight(
                Id: $"colheita-perdida:{snapshot.Season}:{snapshot.Year}",
                Urgency: snapshot.DaysLeftInSeason <= 7 ? Urgency.Today : Urgency.Soon,
                Title: $"{crops.WontMatureTotal} plantacoes nao dao tempo de colher",
                Detail: $"{string.Join(", ", detail)} · morrem na virada para {NextSeasonPt(snapshot.Season)}.",
                Consequence: "arranque e replante com algo que dê tempo",
                Source: "Colheita"
            );
        }

        if (crops.ReadyToHarvest > 0)
        {
            yield return new Insight(
                Id: $"colheita-pronta:{snapshot.Season}:{snapshot.DayOfMonth}:{snapshot.Year}",
                Urgency: Urgency.Info,
                Title: $"{crops.ReadyToHarvest} plantacoes prontas para colher",
                Detail: "",
                Consequence: "",
                Source: "Colheita"
            );
        }

        if (crops.ReadyTomorrow > 0)
        {
            yield return new Insight(
                Id: $"colheita-amanha:{snapshot.Season}:{snapshot.DayOfMonth}:{snapshot.Year}",
                Urgency: Urgency.Info,
                Title: $"{crops.ReadyTomorrow} plantacoes ficam prontas amanha",
                Detail: "",
                Consequence: "",
                Source: "Colheita"
            );
        }
    }

    private static string NextSeasonPt(string season)
    {
        return season?.ToLowerInvariant() switch
        {
            "spring" => "Verao",
            "summer" => "Outono",
            "fall" => "Inverno",
            _ => "Primavera"
        };
    }
}
