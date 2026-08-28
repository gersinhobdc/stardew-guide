namespace Pelican.Model;

/// <summary>Um slot de bundle: o que ele pede.</summary>
/// <param name="ItemId">Id nao-qualificado. Negativo = categoria inteira (ex: -4 = qualquer peixe).</param>
/// <param name="Quantity">Quantos.</param>
/// <param name="Quality">Qualidade minima (0 normal, 1 prata, 2 ouro, 4 iridio).</param>
/// <param name="DisplayName">Nome ja resolvido para exibicao.</param>
public sealed record BundleSlot(string ItemId, int Quantity, int Quality, string DisplayName)
{
    public bool IsCategory => this.ItemId.StartsWith('-');

    public string QualityLabel => this.Quality switch
    {
        1 => " (prata+)",
        2 => " (ouro+)",
        4 => " (iridio)",
        _ => ""
    };

    public string QuantityLabel => this.Quantity > 1 ? $" x{this.Quantity}" : "";

    public override string ToString() => $"{this.DisplayName}{this.QuantityLabel}{this.QualityLabel}";
}

/// <summary>Um bundle do Centro Comunitario, ja cruzado com o estado de conclusao.</summary>
public sealed class BundleInfo
{
    /// <summary>Chave crua vinda do jogo, ex "Pantry/0".</summary>
    public string Key { get; init; } = "";

    /// <summary>Sala, ex "Pantry".</summary>
    public string Room { get; init; } = "";

    /// <summary>Indice global do bundle, usado para casar com CommunityCenter.bundles.</summary>
    public int Index { get; init; }

    /// <summary>Nome interno.</summary>
    public string Name { get; init; } = "";

    /// <summary>Nome traduzido, quando o jogo fornece.</summary>
    public string DisplayName { get; init; } = "";

    /// <summary>String crua da recompensa, ex "O 388 50".</summary>
    public string RewardRaw { get; init; } = "";

    /// <summary>Recompensa ja legivel, ex "50x Madeira".</summary>
    public string RewardLabel { get; init; } = "";

    /// <summary>Quantos slots precisam ser preenchidos (pode ser menor que Slots.Count).</summary>
    public int SlotsRequired { get; init; }

    public IReadOnlyList<BundleSlot> Slots { get; init; } = Array.Empty<BundleSlot>();

    /// <summary>Paralelo a Slots: quais ja foram entregues. Vem do estado do jogo.</summary>
    public IReadOnlyList<bool> Completed { get; init; } = Array.Empty<bool>();

    public int CompletedCount => this.Completed.Count(static c => c);

    public bool IsComplete => this.CompletedCount >= this.SlotsRequired;

    /// <summary>Quantos slots ainda faltam para fechar o bundle.</summary>
    public int Remaining => Math.Max(0, this.SlotsRequired - this.CompletedCount);

    /// <summary>Slots ainda nao entregues.</summary>
    public IEnumerable<BundleSlot> MissingSlots
    {
        get
        {
            for (int i = 0; i < this.Slots.Count; i++)
            {
                if (i >= this.Completed.Count || !this.Completed[i])
                    yield return this.Slots[i];
            }
        }
    }

    public string ProgressBar()
    {
        int filled = Math.Clamp(this.CompletedCount, 0, this.SlotsRequired);
        int empty = Math.Max(0, this.SlotsRequired - filled);
        return new string('#', filled) + new string('.', empty);
    }
}
