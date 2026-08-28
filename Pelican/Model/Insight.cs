namespace Pelican.Model;

/// <summary>Quanto custa ignorar este aviso. Ordem importa: e por ela que o HUD prioriza.</summary>
public enum Urgency
{
    /// <summary>Bom saber, sem prazo.</summary>
    Info = 0,

    /// <summary>Acaba nesta estacao / nos proximos dias.</summary>
    Soon = 1,

    /// <summary>A janela e hoje. Amanha nao da mais.</summary>
    Today = 2,

    /// <summary>Se passar, nao volta mais neste save.</summary>
    Missable = 3
}

/// <summary>
/// A unica unidade de saida do sistema. Todo advisor produz isto e nada mais;
/// o HUD so sabe renderizar isto. Adicionar capacidade = adicionar advisor,
/// nunca mexer no HUD.
/// </summary>
/// <param name="Id">Estavel entre dias, para o UsageLog conseguir casar aviso com acao.</param>
/// <param name="Urgency">Prioridade de renderizacao.</param>
/// <param name="Title">Uma linha, curta. Aparece no HUD compacto.</param>
/// <param name="Detail">Onde/quando/como. Aparece no painel completo.</param>
/// <param name="Consequence">O que isto desbloqueia. E o que diferencia o Pelican dos outros mods.</param>
/// <param name="Source">Advisor de origem, para diagnostico.</param>
/// <param name="RelatedItemIds">Itens que este aviso menciona, para o UsageLog detectar que voce agiu.</param>
public sealed record Insight(
    string Id,
    Urgency Urgency,
    string Title,
    string Detail = "",
    string Consequence = "",
    string Source = "",
    IReadOnlyList<string>? RelatedItemIds = null
)
{
    public IReadOnlyList<string> Items => this.RelatedItemIds ?? Array.Empty<string>();

    /// <summary>Linha unica para o HUD compacto.</summary>
    public string ToCompactLine()
    {
        return string.IsNullOrWhiteSpace(this.Consequence)
            ? this.Title
            : $"{this.Title}  ->  {this.Consequence}";
    }
}
