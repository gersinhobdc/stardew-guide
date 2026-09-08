namespace Pelican.Model;

/// <summary>
/// Um segredo, evento perdivel ou interacao escondida.
/// Curado a mao porque o jogo nao marca nada como "segredo" nem como "perdivel".
/// Campos de janela sao todos opcionais: sem janela = sempre relevante, sem urgencia.
/// </summary>
public sealed class Secret
{
    public string Id { get; set; } = "";
    public string Titulo { get; set; } = "";

    /// <summary>"spring" | "summer" | "fall" | "winter". Nulo = qualquer estacao.</summary>
    public string? Estacao { get; set; }

    /// <summary>Primeiro dia da janela. Nulo = dia 1.</summary>
    public int? DiaInicio { get; set; }

    /// <summary>Ultimo dia da janela. Nulo = dia 28.</summary>
    public int? DiaFim { get; set; }

    /// <summary>Ano minimo em que isto pode acontecer.</summary>
    public int? AnoMin { get; set; }

    /// <summary>
    /// Id do item que este segredo rende, quando ha um. Se voce ja pescou/obteve,
    /// o aviso para de aparecer — um lembrete que se repete depois de resolvido
    /// e a forma mais rapida de te ensinar a ignorar o HUD.
    /// </summary>
    public string? ItemId { get; set; }

    /// <summary>Com quantos dias de antecedencia comecar a avisar.</summary>
    public int AvisarAntes { get; set; }

    /// <summary>"missable" | "today" | "soon" | "info".</summary>
    public string Urgencia { get; set; } = "info";

    public string Detalhe { get; set; } = "";
    public string Consequencia { get; set; } = "";

    /// <summary>Tem janela de data? Sem janela, so aparece no painel completo.</summary>
    public bool HasWindow => this.Estacao != null || this.DiaInicio.HasValue || this.AnoMin.HasValue;

    public Urgency ParsedUrgency => this.Urgencia?.ToLowerInvariant() switch
    {
        "missable" => Urgency.Missable,
        "today" => Urgency.Today,
        "soon" => Urgency.Soon,
        _ => Urgency.Info
    };
}

/// <summary>Envelope do secrets.json.</summary>
public sealed class SecretsFile
{
    public string Descricao { get; set; } = "";
    public List<Secret> Segredos { get; set; } = new();
}
