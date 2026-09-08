using StardewModdingAPI;
using StardewValley;

namespace Pelican.Data;

/// <summary>
/// Quem ama qual presente, lido de Data/NPCGiftTastes.
///
/// Formato da linha, separado por '/':
///   [0] fala  [1] itens AMADOS  [2] fala  [3] itens curtidos  ... e assim por diante
/// Apenas os amados interessam: sao os que valem 8x amizade no aniversario.
/// </summary>
public static class GiftTastes
{
    /// <summary>Monta o indice inverso: id de item -> NPCs que amam aquele item.</summary>
    public static IReadOnlyDictionary<string, List<string>> LovedBy(IGameContentHelper content, IMonitor monitor)
    {
        var result = new Dictionary<string, List<string>>();

        try
        {
            var raw = content.Load<Dictionary<string, string>>("Data/NPCGiftTastes");

            foreach ((string npc, string line) in raw)
            {
                // Entradas "Universal_*" sao regras por categoria, nao pessoas.
                if (npc.StartsWith("Universal_", StringComparison.OrdinalIgnoreCase))
                    continue;

                string[] parts = line.Split('/');
                if (parts.Length < 2)
                    continue;

                foreach (string itemId in parts[1].Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!result.TryGetValue(itemId, out List<string>? npcs))
                        result[itemId] = npcs = new List<string>();

                    npcs.Add(DisplayName(npc));
                }
            }
        }
        catch (Exception ex)
        {
            monitor.Log($"Nao consegui ler Data/NPCGiftTastes: {ex.Message}", LogLevel.Trace);
        }

        return result;
    }

    /// <summary>Indice direto: NPC -> nomes dos itens que ele ama.</summary>
    public static IReadOnlyDictionary<string, List<string>> LovedItemsPerNpc(IGameContentHelper content, IMonitor monitor)
    {
        var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var raw = content.Load<Dictionary<string, string>>("Data/NPCGiftTastes");

            foreach ((string npc, string line) in raw)
            {
                if (npc.StartsWith("Universal_", StringComparison.OrdinalIgnoreCase))
                    continue;

                string[] parts = line.Split('/');
                if (parts.Length < 2)
                    continue;

                var names = parts[1]
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Select(ItemNames.Resolve)
                    .Where(static n => !string.IsNullOrWhiteSpace(n))
                    .Distinct()
                    .ToList();

                if (names.Count > 0)
                    result[npc] = names;
            }
        }
        catch (Exception ex)
        {
            monitor.Log($"Nao consegui ler os presentes por NPC: {ex.Message}", LogLevel.Trace);
        }

        return result;
    }

    private static string DisplayName(string internalName)
    {
        try
        {
            NPC? npc = Game1.getCharacterFromName(internalName);
            if (npc != null && !string.IsNullOrWhiteSpace(npc.displayName))
                return npc.displayName;
        }
        catch
        {
            // NPC ainda nao carregado; o nome interno serve.
        }

        return internalName;
    }
}
