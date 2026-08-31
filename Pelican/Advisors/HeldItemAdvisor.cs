using Pelican.Data;
using Pelican.Model;
using StardewModdingAPI;
using StardewValley;

namespace Pelican.Advisors;

/// <summary>
/// "O que eu faço com isto que está na minha mão?"
///
/// Responde a pergunta que voce faz o tempo todo jogando, sem abrir menu nenhum:
/// serve pra bundle? alguem ama de presente? o museu ainda aceita?
///
/// Le tudo do jogo (Data/NPCGiftTastes, bundles, LibraryMuseum), entao itens
/// novos de versao futura aparecem sozinhos, sem curadoria.
/// </summary>
public sealed class HeldItemAdvisor : IAdvisor
{
    private readonly IGameContentHelper Content;
    private readonly IMonitor Monitor;
    private IReadOnlyDictionary<string, List<string>>? LovedCache;

    public HeldItemAdvisor(IGameContentHelper content, IMonitor monitor)
    {
        this.Content = content;
        this.Monitor = monitor;
    }

    public string Name => "ItemNaMao";

    public void InvalidateCache() => this.LovedCache = null;

    public IEnumerable<Insight> Advise(GameSnapshot snapshot)
    {
        Item? item = snapshot.HeldItem;
        if (item is null)
            return Array.Empty<Insight>();

        var results = new List<Insight>();
        string name = SafeName(item);

        results.AddRange(this.BundleUse(snapshot, item, name));
        results.AddRange(this.GiftUse(item, name));

        if (snapshot.HeldItemDonatable)
        {
            results.Add(new Insight(
                Id: $"mao-museu:{item.QualifiedItemId}",
                Urgency: Urgency.Info,
                Title: $"{name}: o museu ainda nao tem este",
                Detail: "Leve ao Gunther. Doacoes contam para a Chave Enferrujada (60 itens) e para a Perfeicao.",
                Consequence: "doavel ao museu",
                Source: "ItemNaMao"
            ));
        }

        return results;
    }

    /// <summary>Serve para algum bundle que ainda falta? Com a cadeia de consequencia.</summary>
    private IEnumerable<Insight> BundleUse(GameSnapshot snapshot, Item item, string name)
    {
        foreach (BundleInfo bundle in snapshot.IncompleteBundles)
        {
            BundleSlot? slot = bundle.MissingSlots.FirstOrDefault(s => ItemNames.Matches(item, s));
            if (slot is null)
                continue;

            RoomReward? reward = snapshot.RewardFor(bundle.Room);
            string roomName = reward?.Nome ?? bundle.Room;
            string bundleName = !string.IsNullOrWhiteSpace(bundle.DisplayName) ? bundle.DisplayName : bundle.Name;

            bool lastSlot = bundle.Remaining == 1;
            bool closesRoom = lastSlot && snapshot.WouldCompleteRoom(bundle);

            string consequence = closesRoom && reward is not null
                ? $"fecha {roomName} -> {reward.Destrava}"
                : lastSlot
                    ? $"fecha o bundle -> {Fallback(bundle.RewardLabel, roomName)}"
                    : $"{bundle.ProgressBar()} em {roomName}";

            yield return new Insight(
                Id: $"mao-bundle:{bundle.Key}",
                Urgency: closesRoom ? Urgency.Today : Urgency.Info,
                Title: $"{name} -> {bundleName}",
                Detail: slot.Quantity > 1
                    ? $"O bundle pede {slot.Quantity}x{slot.QualityLabel}."
                    : $"Ainda falta neste bundle{slot.QualityLabel}.",
                Consequence: consequence,
                Source: "ItemNaMao",
                RelatedItemIds: new[] { slot.ItemId }
            );
        }
    }

    /// <summary>Alguem ama isto de presente?</summary>
    private IEnumerable<Insight> GiftUse(Item item, string name)
    {
        this.LovedCache ??= GiftTastes.LovedBy(this.Content, this.Monitor);

        string bare = Unqualify(item.QualifiedItemId);
        if (!this.LovedCache.TryGetValue(bare, out List<string>? npcs) || npcs.Count == 0)
            yield break;

        yield return new Insight(
            Id: $"mao-presente:{item.QualifiedItemId}",
            Urgency: Urgency.Info,
            Title: $"{name}: presente amado de {string.Join(", ", npcs.Take(4))}",
            Detail: npcs.Count > 4 ? $"e mais {npcs.Count - 4}." : "",
            Consequence: "8x amizade se for no aniversario",
            Source: "ItemNaMao"
        );
    }

    private static string Unqualify(string qualifiedId)
    {
        int close = qualifiedId.IndexOf(')');
        return close >= 0 ? qualifiedId[(close + 1)..] : qualifiedId;
    }

    private static string SafeName(Item item)
    {
        try
        {
            return string.IsNullOrWhiteSpace(item.DisplayName) ? item.Name : item.DisplayName;
        }
        catch
        {
            return "item";
        }
    }

    private static string Fallback(string primary, string secondary)
    {
        return !string.IsNullOrWhiteSpace(primary) ? primary : secondary;
    }
}
