using Pelican.Model;

namespace Pelican.Advisors;

/// <summary>
/// Avisa antes do ponto de nao retorno.
///
/// Nenhum mod existente faz isso. O jogo nao marca nada como "perdivel", entao
/// esta e a unica parte do Pelican que depende de curadoria (assets/secrets.json).
///
/// A comparacao de datas usa dia absoluto desde o inicio do save, para que um
/// aviso possa cruzar a virada de estacao e de ano - e o que permite avisar no
/// Inverno do Ano 2 sobre a avaliacao do Vovo na Primavera do Ano 3.
/// </summary>
public sealed class MissableAdvisor : IAdvisor
{
    private const int DaysPerSeason = 28;
    private const int SeasonsPerYear = 4;
    private const int DaysPerYear = DaysPerSeason * SeasonsPerYear;

    private readonly IReadOnlyList<Secret> Secrets;

    public MissableAdvisor(IReadOnlyList<Secret> secrets)
    {
        this.Secrets = secrets;
    }

    public string Name => "Perdiveis";

    public IEnumerable<Insight> Advise(GameSnapshot snapshot)
    {
        var results = new List<Insight>();
        int today = Absolute(snapshot.Year, snapshot.Season, snapshot.DayOfMonth);

        foreach (Secret secret in this.Secrets)
        {
            // Ja conseguiu o que este segredo rende? Entao calado.
            if (!string.IsNullOrWhiteSpace(secret.ItemId) && snapshot.HasCaught(secret.ItemId))
                continue;

            // Sem janela: informativo puro, so no painel completo.
            if (!secret.HasWindow)
            {
                results.Add(ToInsight(secret, Urgency.Info, secret.Titulo));
                continue;
            }

            Insight? windowed = Evaluate(secret, snapshot, today);
            if (windowed != null)
                results.Add(windowed);
        }

        return results;
    }

    /// <summary>Avalia a janela do segredo no ano atual e no proximo.</summary>
    private static Insight? Evaluate(Secret secret, GameSnapshot snapshot, int today)
    {
        for (int year = snapshot.Year; year <= snapshot.Year + 1; year++)
        {
            if (secret.AnoMin.HasValue && year < secret.AnoMin.Value)
                continue;

            string season = secret.Estacao ?? snapshot.Season;
            int start = Absolute(year, season, secret.DiaInicio ?? 1);
            int end = Absolute(year, season, secret.DiaFim ?? DaysPerSeason);

            // Dentro da janela agora.
            if (today >= start && today <= end)
            {
                int left = end - today;
                string suffix = left == 0 ? " — ULTIMO DIA" : left <= 2 ? $" — restam {left} dias" : "";

                return ToInsight(
                    secret,
                    left <= 2 ? Urgency.Missable : secret.ParsedUrgency,
                    secret.Titulo + suffix
                );
            }

            // Ainda vai chegar, mas ja e hora de avisar.
            if (today < start)
            {
                int until = start - today;
                if (secret.AvisarAntes > 0 && until <= secret.AvisarAntes)
                {
                    return ToInsight(
                        secret,
                        secret.ParsedUrgency,
                        $"{secret.Titulo} — em {until} dia{(until == 1 ? "" : "s")}"
                    );
                }

                // A proxima ocorrencia e no futuro e ainda nao e hora. Nao ha o que avaliar
                // nos anos seguintes, porque eles so estao mais longe.
                return null;
            }
        }

        return null;
    }

    private static Insight ToInsight(Secret secret, Urgency urgency, string title)
    {
        return new Insight(
            Id: $"segredo:{secret.Id}",
            Urgency: urgency,
            Title: title,
            Detail: secret.Detalhe,
            Consequence: secret.Consequencia,
            Source: "Perdiveis"
        );
    }

    /// <summary>Dia absoluto desde o inicio do save, para comparar datas atravessando estacoes.</summary>
    private static int Absolute(int year, string season, int day)
    {
        return ((year - 1) * DaysPerYear)
            + (SeasonIndex(season) * DaysPerSeason)
            + day;
    }

    private static int SeasonIndex(string season)
    {
        return season?.ToLowerInvariant() switch
        {
            "spring" => 0,
            "summer" => 1,
            "fall" => 2,
            "winter" => 3,
            _ => 0
        };
    }
}
