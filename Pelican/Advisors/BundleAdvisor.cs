using Pelican.Data;
using Pelican.Model;
using StardewValley;

namespace Pelican.Advisors;

/// <summary>
/// Cruza o que voce esta carregando com o que o Centro Comunitario ainda pede.
///
/// A diferenca para os mods existentes (Community Center Companion e afins) e a
/// CADEIA DE CONSEQUENCIA: eles dizem "este item e de um bundle". Aqui o aviso e
/// "Enguia -> fecha Tanque de Peixes -> libera a Bateia".
/// </summary>
public sealed class BundleAdvisor : IAdvisor
{
    public string Name => "Bundles";

    public IEnumerable<Insight> Advise(GameSnapshot snapshot)
    {
        var insights = new List<Insight>();

        foreach (BundleInfo bundle in snapshot.IncompleteBundles)
        {
            var missing = bundle.MissingSlots.ToList();
            if (missing.Count == 0)
                continue;

            // Quais slots faltantes o inventario ja cobre?
            var covered = missing
                .Where(slot => CountMatching(snapshot.Inventory, slot) >= slot.Quantity)
                .ToList();

            if (covered.Count == 0)
                continue;

            bool completesBundle = covered.Count >= bundle.Remaining;
            bool completesRoom = completesBundle && snapshot.WouldCompleteRoom(bundle);

            insights.Add(BuildInsight(snapshot, bundle, covered, completesBundle, completesRoom));
        }

        // Fechar sala primeiro, depois fechar bundle, depois o resto.
        return insights.OrderByDescending(static i => i.Urgency);
    }

    private static Insight BuildInsight(
        GameSnapshot snapshot,
        BundleInfo bundle,
        IReadOnlyList<BundleSlot> covered,
        bool completesBundle,
        bool completesRoom)
    {
        string bundleName = Prettify(bundle.DisplayName, bundle.Name);
        string items = string.Join(", ", covered.Select(static s => s.ToString()));

        RoomReward? reward = snapshot.RewardFor(bundle.Room);
        string roomName = reward?.Nome ?? bundle.Room;

        string title;
        string consequence;
        Urgency urgency;

        if (completesRoom)
        {
            title = $"{items} FECHA a sala {roomName}";
            consequence = reward is null
                ? $"conclui {roomName}"
                : $"destrava: {reward.Destrava}";
            urgency = Urgency.Today;
        }
        else if (completesBundle)
        {
            title = $"{items} fecha o bundle {bundleName}";
            consequence = string.IsNullOrWhiteSpace(bundle.RewardLabel)
                ? $"avanca {roomName}"
                : $"recompensa: {bundle.RewardLabel}";
            urgency = Urgency.Soon;
        }
        else
        {
            int stillMissing = bundle.Remaining - covered.Count;
            title = $"{items} vai para {bundleName}";
            consequence = $"ainda faltariam {stillMissing} em {roomName}";
            urgency = Urgency.Info;
        }

        string detail = BuildDetail(bundle, covered, reward);

        return new Insight(
            Id: $"bundle:{bundle.Key}",
            Urgency: urgency,
            Title: title,
            Detail: detail,
            Consequence: consequence,
            Source: "Bundles",
            RelatedItemIds: covered.Select(static s => s.ItemId).ToList()
        );
    }

    private static string BuildDetail(BundleInfo bundle, IReadOnlyList<BundleSlot> covered, RoomReward? reward)
    {
        var parts = new List<string>
        {
            $"{bundle.ProgressBar()} {bundle.CompletedCount}/{bundle.SlotsRequired}"
        };

        var stillMissing = bundle.MissingSlots
            .Where(slot => !covered.Contains(slot))
            .Select(static s => s.ToString())
            .ToList();

        if (stillMissing.Count > 0)
            parts.Add($"ainda falta: {string.Join(", ", stillMissing)}");

        if (reward is not null && !string.IsNullOrWhiteSpace(reward.Detalhe))
            parts.Add(reward.Detalhe);

        return string.Join(" · ", parts);
    }

    /// <summary>Quantos itens do inventario satisfazem este slot, somando as pilhas.</summary>
    private static int CountMatching(IReadOnlyList<Item> inventory, BundleSlot slot)
    {
        int total = 0;

        foreach (Item item in inventory)
        {
            if (ItemNames.Matches(item, slot))
                total += Math.Max(1, item.Stack);
        }

        return total;
    }

    /// <summary>Nomes internos vem em ingles e as vezes vazios; cai para o que existir.</summary>
    private static string Prettify(string displayName, string fallback)
    {
        return !string.IsNullOrWhiteSpace(displayName) ? displayName : fallback;
    }
}
