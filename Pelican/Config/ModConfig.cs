using StardewModdingAPI;

namespace Pelican.Config;

/// <summary>
/// Config do Pelican. Gerada em config.json na primeira execucao.
///
/// Os interruptores por advisor existem por um motivo especifico: o Pelican
/// convive com UI Info Suite 2 e Community Center Companion. Se ele repetir o
/// que eles ja mostram, o HUD vira poluicao e voce para de ler. Desligue o que
/// sobrepuser.
/// </summary>
public sealed class ModConfig
{
    // ---- HUD ----

    /// <summary>Painel compacto sempre visivel no canto.</summary>
    public bool MostrarHudCompacto { get; set; } = true;

    /// <summary>Quantas linhas o painel compacto mostra. Acima de ~6 vira ruido.</summary>
    public int MaxLinhasHud { get; set; } = 4;

    /// <summary>
    /// Largura maxima do painel compacto, em caracteres. O texto e cortado com "..."
    /// para o HUD nunca tomar a tela. O F9 mostra o texto inteiro.
    /// </summary>
    public int MaxCaracteresPorLinha { get; set; } = 62;

    /// <summary>
    /// Esconder a consequencia ("-> libera a Bateia") no painel compacto.
    /// Deixa o HUD bem menor; a consequencia continua no F9.
    /// </summary>
    public bool HudCompactoSoTitulo { get; set; } = false;

    /// <summary>Canto do painel compacto: "TopLeft", "TopRight", "BottomLeft", "BottomRight".</summary>
    public string CantoHud { get; set; } = "TopLeft";

    /// <summary>Primeira linha fixa do HUD com data, clima e sorte do dia.</summary>
    public bool MostrarLinhaDeStatus { get; set; } = true;

    /// <summary>Abre o painel completo / quadro de co-op.</summary>
    public SButton TeclaPainel { get; set; } = SButton.F9;

    /// <summary>Esconde ou mostra o painel compacto do canto.</summary>
    public SButton TeclaEsconder { get; set; } = SButton.F8;

    /// <summary>Marca o bau sob o cursor como bau do Centro Comunitario.</summary>
    public SButton TeclaMarcarBau { get; set; } = SButton.F10;

    // ---- Advisors ----

    /// <summary>Itens no inventario que fecham bundle, com a cadeia de consequencia.</summary>
    public bool AvisarBundles { get; set; } = true;

    /// <summary>Bundles prontos para entregar a partir do bau marcado.</summary>
    public bool AvisarBau { get; set; } = true;

    /// <summary>Fim de estacao, ultimo dia de plantio, aniversarios, festivais.</summary>
    public bool AvisarCalendario { get; set; } = true;

    /// <summary>Cruza o clima e a sorte de hoje com o que ainda falta. Nao repete o
    /// icone de sorte do UI Info Suite 2: aqui o aviso e "chove e voce precisa de Enguia".</summary>
    public bool AvisarClimaSorte { get; set; } = true;

    /// <summary>Eventos de uma vez so, que nao voltam mais neste save.</summary>
    public bool AvisarPerdiveis { get; set; } = true;

    /// <summary>Para que serve o item selecionado na mao: bundle, presente amado, museu.</summary>
    public bool AvisarItemNaMao { get; set; } = true;

    /// <summary>Plantacoes prontas e, principalmente, as que nao dao tempo de colher.</summary>
    public bool AvisarColheita { get; set; } = true;

    /// <summary>Quanto falta para as recompensas do museu.</summary>
    public bool AvisarColecao { get; set; } = true;

    // ---- Bau de staging ----

    /// <summary>Nome interno da localizacao do bau. Preenchido pela tecla de marcar.</summary>
    public string BauLocal { get; set; } = "";

    public int BauX { get; set; } = -1;
    public int BauY { get; set; } = -1;

    public bool TemBauMarcado => !string.IsNullOrWhiteSpace(this.BauLocal) && this.BauX >= 0 && this.BauY >= 0;

    // ---- Instrumentacao ----

    /// <summary>
    /// Grava usage.jsonl: quais avisos apareceram e se voce agiu neles.
    /// E o que decide o Portao 1 sem depender de voce manter um diario a mao.
    /// </summary>
    public bool RegistrarUso { get; set; } = true;
}
