using Pelican.Model;
using StardewValley;
using StardewValley.ItemTypeDefinitions;

namespace Pelican.Data;

/// <summary>
/// Resolve id de item em nome legivel. Toda a resolucao passa por aqui para que,
/// se a 1.7 mudar o formato de id, exista um unico lugar para corrigir.
/// </summary>
public static class ItemNames
{
    /// <summary>
    /// Categorias (ids negativos) que bundles usam para dizer "qualquer item deste tipo".
    /// O jogo nao expoe isso como dado consultavel, entao e mapa fixo.
    /// </summary>
    private static readonly Dictionary<string, string> Categories = new()
    {
        ["-2"] = "Qualquer gema",
        ["-4"] = "Qualquer peixe",
        ["-5"] = "Qualquer ovo",
        ["-6"] = "Qualquer leite",
        ["-7"] = "Qualquer prato cozido",
        ["-8"] = "Qualquer item de artesanato",
        ["-12"] = "Qualquer mineral",
        ["-14"] = "Qualquer carne",
        ["-15"] = "Qualquer recurso de metal",
        ["-16"] = "Qualquer recurso de construcao",
        ["-19"] = "Qualquer fertilizante",
        ["-20"] = "Qualquer lixo",
        ["-21"] = "Qualquer isca",
        ["-22"] = "Qualquer chamariz",
        ["-24"] = "Qualquer mobilia",
        ["-25"] = "Qualquer ingrediente",
        ["-26"] = "Qualquer produto artesanal",
        ["-27"] = "Qualquer xarope",
        ["-28"] = "Qualquer espolio de monstro",
        ["-74"] = "Qualquer semente",
        ["-75"] = "Qualquer vegetal",
        ["-79"] = "Qualquer fruta",
        ["-80"] = "Qualquer flor",
        ["-81"] = "Qualquer forragem",
        ["-95"] = "Qualquer chapeu",
        ["-96"] = "Qualquer anel",
        ["-98"] = "Qualquer arma",
        ["-99"] = "Qualquer ferramenta",
        ["-777"] = "Sementes selvagens"
    };

    /// <summary>Nome legivel de um id de bundle (que pode ser categoria negativa).</summary>
    public static string Resolve(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return "?";

        itemId = itemId.Trim();

        if (Categories.TryGetValue(itemId, out string? category))
            return category;

        return ResolveQualified(Qualify(itemId));
    }

    /// <summary>Nome legivel de um id ja qualificado, ex "(O)388".</summary>
    public static string ResolveQualified(string qualifiedId)
    {
        try
        {
            ParsedItemData? data = ItemRegistry.GetData(qualifiedId);
            if (data != null && !string.IsNullOrWhiteSpace(data.DisplayName))
                return data.DisplayName;
        }
        catch
        {
            // Id desconhecido nao pode derrubar o HUD. Cai para o id cru.
        }

        return qualifiedId;
    }

    /// <summary>Transforma id nao-qualificado em qualificado, assumindo objeto.</summary>
    public static string Qualify(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return itemId;

        return itemId.StartsWith('(')
            ? itemId
            : ItemRegistry.type_object + itemId;
    }

    /// <summary>
    /// Um item do inventario satisfaz um slot de bundle? Trata categoria, qualidade e id.
    /// </summary>
    public static bool Matches(Item item, BundleSlot slot)
    {
        if (item is null)
            return false;

        try
        {
            // Qualidade: o item precisa ser pelo menos tao bom quanto o slot pede.
            int quality = item is StardewValley.Object obj ? obj.Quality : 0;
            if (quality < slot.Quality)
                return false;

            if (slot.IsCategory)
            {
                return int.TryParse(slot.ItemId, out int categoryId)
                    && item.Category == categoryId;
            }

            string wanted = Qualify(slot.ItemId);
            return item.QualifiedItemId == wanted;
        }
        catch
        {
            return false;
        }
    }
}
