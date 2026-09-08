using Pelican.Model;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;

namespace Pelican.Data;

/// <summary>
/// Le os bundles do Centro Comunitario direto do estado do jogo.
///
/// Fonte: Game1.netWorldState.Value.BundleData (definicao) cruzada com
/// CommunityCenter.bundles (o que ja foi entregue). Nao ha dataset curado:
/// bundles remixados e mudancas de versao vem de graca.
///
/// Formato do valor, separado por '/':
///   [0] nome  [1] recompensa  [2] itens  [3] cor  [4] qtd exigida  [5+] nome de exibicao
/// Campos a partir do 4 variam entre versoes, entao a leitura e tolerante por design.
/// </summary>
public static class BundleReader
{
    /// <summary>Mapeia o prefixo de recompensa para o tipo de item do registry.</summary>
    private static readonly Dictionary<string, string> RewardTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["O"] = "(O)",
        ["BO"] = "(BC)",
        ["R"] = "(R)",
        ["W"] = "(W)",
        ["H"] = "(H)",
        ["B"] = "(B)",
        ["BL"] = "(B)",
        ["F"] = "(F)",
        ["T"] = "(T)"
    };

    /// <summary>Le todos os bundles. Nunca lanca: erro vira log e lista vazia.</summary>
    public static IReadOnlyList<BundleInfo> ReadAll(IMonitor monitor)
    {
        var result = new List<BundleInfo>();

        try
        {
            Dictionary<string, string>? bundleData = Game1.netWorldState?.Value?.BundleData;
            if (bundleData is null || bundleData.Count == 0)
            {
                monitor.Log("BundleData vazio ou indisponivel; o save ja foi carregado?", LogLevel.Trace);
                return result;
            }

            CommunityCenter? cc = Game1.getLocationFromName("CommunityCenter") as CommunityCenter;

            // Sem Centro Comunitario (rota Joja), nao existe bundle a completar.
            // Sem esta guarda o mod leria "nada entregue" e anunciaria TODOS os
            // bundles como pendentes — conselho falso, que e pior que silencio.
            if (cc is null)
            {
                monitor.LogOnce("Centro Comunitario nao encontrado (rota Joja?); avisos de bundle desligados.", LogLevel.Info);
                return result;
            }

            foreach ((string key, string raw) in bundleData)
            {
                BundleInfo? parsed = ParseOne(key, raw, cc, monitor);
                if (parsed != null)
                    result.Add(parsed);
            }
        }
        catch (Exception ex)
        {
            monitor.Log($"Falha ao ler bundles: {ex.Message}", LogLevel.Warn);
        }

        return result;
    }

    private static BundleInfo? ParseOne(string key, string raw, CommunityCenter? cc, IMonitor monitor)
    {
        try
        {
            string[] keyParts = key.Split('/');
            string room = keyParts.Length > 0 ? keyParts[0] : "?";
            int index = keyParts.Length > 1 && int.TryParse(keyParts[1], out int i) ? i : -1;

            string[] parts = raw.Split('/');
            if (parts.Length < 3)
            {
                monitor.Log($"Bundle '{key}' com formato inesperado ({parts.Length} campos); ignorado.", LogLevel.Trace);
                return null;
            }

            string name = parts[0];
            string rewardRaw = parts[1];
            IReadOnlyList<BundleSlot> slots = ParseSlots(parts[2]);

            // Campo 4 e a quantidade exigida, mas nem sempre existe e nem sempre e numero.
            int slotsRequired = slots.Count;
            if (parts.Length > 4 && int.TryParse(parts[4], out int required) && required > 0)
                slotsRequired = Math.Min(required, slots.Count);

            // O nome de exibicao e o ultimo campo nao-numerico, quando existe.
            string displayName = name;
            for (int p = parts.Length - 1; p >= 5; p--)
            {
                if (!string.IsNullOrWhiteSpace(parts[p]) && !int.TryParse(parts[p], out _))
                {
                    displayName = parts[p];
                    break;
                }
            }

            IReadOnlyList<bool> completed = ReadCompletion(cc, index, slots.Count);

            return new BundleInfo
            {
                Key = key,
                Room = room,
                Index = index,
                Name = name,
                DisplayName = displayName,
                RewardRaw = rewardRaw,
                RewardLabel = DescribeReward(rewardRaw),
                SlotsRequired = slotsRequired,
                Slots = slots,
                Completed = completed
            };
        }
        catch (Exception ex)
        {
            monitor.Log($"Falha ao parsear bundle '{key}': {ex.Message}", LogLevel.Trace);
            return null;
        }
    }

    /// <summary>Itens vem como trincas "id qtd qualidade" separadas por espaco.</summary>
    private static IReadOnlyList<BundleSlot> ParseSlots(string itemsRaw)
    {
        var slots = new List<BundleSlot>();

        string[] tokens = itemsRaw.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i + 2 < tokens.Length; i += 3)
        {
            string itemId = tokens[i];
            int quantity = int.TryParse(tokens[i + 1], out int q) ? q : 1;
            int quality = int.TryParse(tokens[i + 2], out int ql) ? ql : 0;

            slots.Add(new BundleSlot(itemId, quantity, quality, ItemNames.Resolve(itemId)));
        }

        return slots;
    }

    /// <summary>Quais slots ja foram entregues, direto do estado sincronizado do jogo.</summary>
    private static IReadOnlyList<bool> ReadCompletion(CommunityCenter? cc, int index, int slotCount)
    {
        if (cc is null || index < 0)
            return new bool[slotCount];

        try
        {
            if (cc.bundles.ContainsKey(index))
            {
                bool[] state = cc.bundles[index];
                if (state.Length >= slotCount)
                    return state;

                // Defensivo: se o jogo trouxer menos slots que a definicao, completa com false.
                var padded = new bool[slotCount];
                Array.Copy(state, padded, state.Length);
                return padded;
            }
        }
        catch
        {
            // Estado indisponivel (save nao carregado, CC ja concluido). Trata como nada entregue.
        }

        return new bool[slotCount];
    }

    /// <summary>"O 388 50" vira "50x Madeira".</summary>
    public static string DescribeReward(string rewardRaw)
    {
        try
        {
            string[] parts = rewardRaw.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                return "";

            if (!RewardTypes.TryGetValue(parts[0], out string? type))
                return "";

            string name = ItemNames.ResolveQualified(type + parts[1]);
            int quantity = parts.Length > 2 && int.TryParse(parts[2], out int q) ? q : 1;

            return quantity > 1 ? $"{quantity}x {name}" : name;
        }
        catch
        {
            return "";
        }
    }
}
