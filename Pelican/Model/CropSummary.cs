namespace Pelican.Model;

/// <summary>
/// Situacao das plantacoes, resumida.
///
/// O aviso que importa aqui nao e "faltam N dias" (o jogo ja mostra isso na
/// plantacao). E "esta cultura NAO vai amadurecer antes da virada" - dinheiro
/// e tempo ja gastos que vao morrer com a estacao, e que nada no jogo avisa.
/// </summary>
public sealed class CropSummary
{
    /// <summary>Quantas estao prontas para colher agora.</summary>
    public int ReadyToHarvest { get; init; }

    /// <summary>Quantas ficam prontas amanha.</summary>
    public int ReadyTomorrow { get; init; }

    /// <summary>Nome -> quantidade de culturas que nao amadurecem antes da virada.</summary>
    public IReadOnlyDictionary<string, int> WontMature { get; init; }
        = new Dictionary<string, int>();

    public int WontMatureTotal => this.WontMature.Values.Sum();
}
