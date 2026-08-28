using Pelican.Data;
using Pelican.Model;
using StardewValley;

namespace Pelican.Advisors;

/// <summary>
/// O recurso que nao existe em nenhum outro mod.
///
/// Voce marca um bau como "bau do Centro Comunitario" (tecla configuravel, padrao F10).
/// A partir dai o Pelican credita o que esta guardado ali contra o que falta e avisa
/// quando um bundle esta COMPLETO no bau e so falta levar ate o Centro Comunitario.
///
/// Resolve o problema real: voce guarda o item pensando "depois eu levo",
/// e tres estacoes depois ele continua la.
/// </summary>
public sealed class ChestStagingAdvisor : IAdvisor
{
    public string Name => "Bau";

    public IEnumerable<Insight> Advise(GameSnapshot snapshot)
    {
        if (!snapshot.HasStagingChest)
            return Array.Empty<Insight>();

        var insights = new List<Insight>();

        foreach (BundleInfo bundle in snapshot.IncompleteBundles)
        {
            var missing = bundle.MissingSlots.ToList();
            if (missing.Count == 0)
                continue;

            var inChest = missing
                .Where(slot => CountMatching(snapshot.StagedItems, slot) >= slot.Quantity)
                .ToList();

            if (inChest.Count == 0)
                continue;

            bool readyFromChest = inChest.Count >= bundle.Remaining;

            RoomReward? reward = snapshot.RewardFor(bundle.Room);
            string roomName = reward?.Nome ?? bundle.Room;
            string bundleName = !string.IsNullOrWhiteSpace(bundle.DisplayName) ? bundle.DisplayName : bundle.Name;

            if (readyFromChest)
            {
                bool completesRoom = snapshot.WouldCompleteRoom(bundle);

                insights.Add(new Insight(
                    Id: $"bau-pronto:{bundle.Key}",
                    Urgency: Urgency.Today,
                    Title: $"PRONTO NO BAU: {bundleName}",
                    Detail: $"Tudo que falta ja esta no bau: {string.Join(", ", inChest.Select(static s => s.ToString()))}. So levar ao Centro Comunitario.",
                    Consequence: completesRoom && reward is not null
                        ? $"fecha {roomName} -> {reward.Destrava}"
                        : $"fecha o bundle -> {Fallback(bundle.RewardLabel, roomName)}",
                    Source: "Bau",
                    RelatedItemIds: inChest.Select(static s => s.ItemId).ToList()
                ));
            }
            else
            {
                var stillMissing = missing.Except(inChest).ToList();

                insights.Add(new Insight(
                    Id: $"bau-parcial:{bundle.Key}",
                    Urgency: Urgency.Info,
                    Title: $"No bau para {bundleName}: {string.Join(", ", inChest.Select(static s => s.ToString()))}",
                    Detail: $"Falta conseguir: {string.Join(", ", stillMissing.Select(static s => s.ToString()))}",
                    Consequence: $"{bundle.ProgressBar()} em {roomName}",
                    Source: "Bau",
                    RelatedItemIds: stillMissing.Select(static s => s.ItemId).ToList()
                ));
            }
        }

        return insights.OrderByDescending(static i => i.Urgency);
    }

    /// <summary>
    /// Bundles que ficam prontos somando bau + inventario. Usado na notificacao
    /// disparada quando voce guarda algo no bau.
    /// </summary>
    public static IReadOnlyList<BundleInfo> BundlesReadyInChest(GameSnapshot snapshot)
    {
        if (!snapshot.HasStagingChest)
            return Array.Empty<BundleInfo>();

        return snapshot.IncompleteBundles
            .Where(bundle =>
            {
                var missing = bundle.MissingSlots.ToList();
                if (missing.Count == 0)
                    return false;

                int covered = missing.Count(slot => CountMatching(snapshot.StagedItems, slot) >= slot.Quantity);
                return covered >= bundle.Remaining;
            })
            .ToList();
    }

    private static int CountMatching(IReadOnlyList<Item> items, BundleSlot slot)
    {
        int total = 0;

        foreach (Item item in items)
        {
            if (ItemNames.Matches(item, slot))
                total += Math.Max(1, item.Stack);
        }

        return total;
    }

    private static string Fallback(string primary, string secondary)
    {
        return !string.IsNullOrWhiteSpace(primary) ? primary : secondary;
    }
}
