using Pelican.Model;

namespace Pelican.Advisors;

/// <summary>
/// Placar do que ainda falta colecionar.
///
/// Nao lista item por item — isso o menu de Colecoes do jogo ja faz. O que ele
/// nao faz e te lembrar de que voce esta a poucas pecas de uma recompensa
/// concreta, que e o que muda a decisao do dia.
/// </summary>
public sealed class CollectionAdvisor : IAdvisor
{
    /// <summary>Marcos do museu que valem alguma coisa de verdade.</summary>
    private static readonly (int Pieces, string Reward)[] MuseumMilestones =
    {
        (60, "Chave Enferrujada — libera o Esgoto, o Krobus e a Carpa Mutante"),
        (95, "colecao completa do museu")
    };

    public string Name => "Colecao";

    public IEnumerable<Insight> Advise(GameSnapshot snapshot)
    {
        if (snapshot.MuseumDonated <= 0)
            yield break;

        foreach ((int pieces, string reward) in MuseumMilestones)
        {
            if (snapshot.MuseumDonated >= pieces)
                continue;

            int missing = pieces - snapshot.MuseumDonated;

            // So vale falar quando esta perto o bastante para mudar o que voce
            // faz hoje. A 40 pecas de distancia isso e ruido.
            if (missing > 10)
                yield break;

            yield return new Insight(
                Id: $"museu:{pieces}",
                Urgency: Urgency.Info,
                Title: $"Faltam {missing} pecas para: {reward}",
                Detail: $"Museu: {snapshot.MuseumDonated}/{snapshot.MuseumTotal} doados. "
                    + "Geodos do Clint e escavacoes na mina sao a via mais rapida.",
                Consequence: missing <= 3 ? "voce esta perto" : "",
                Source: "Colecao"
            );

            yield break;
        }
    }
}
