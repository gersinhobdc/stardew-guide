using Microsoft.Xna.Framework;
using Pelican.Config;
using Pelican.Model;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;

namespace Pelican.Data;

/// <summary>
/// Monta o GameSnapshot a partir do estado vivo do jogo.
///
/// Este e o unico arquivo que fala com Game1. Se a 1.7 mexer em API de estado,
/// e aqui que quebra e e aqui que se conserta - os advisors nao percebem.
///
/// SOMENTE LEITURA. Nenhuma linha deste arquivo escreve estado de jogo.
/// </summary>
public static class SnapshotBuilder
{
    private static readonly Dictionary<string, string> SeasonNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["spring"] = "Primavera",
        ["summer"] = "Verao",
        ["fall"] = "Outono",
        ["winter"] = "Inverno"
    };

    private static readonly Dictionary<string, string> WeatherNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Sun"] = "Sol",
        ["Rain"] = "Chuva",
        ["Storm"] = "Tempestade",
        ["Snow"] = "Neve",
        ["Wind"] = "Vento",
        ["Festival"] = "Festival",
        ["GreenRain"] = "Chuva Verde"
    };

    public static GameSnapshot Build(
        ModConfig config,
        IReadOnlyDictionary<string, RoomReward> roomRewards,
        IMonitor monitor)
    {
        string season = SafeGet(() => Game1.currentSeason, "spring");

        return new GameSnapshot
        {
            Year = SafeGet(() => Game1.year, 1),
            DayOfMonth = SafeGet(() => Game1.dayOfMonth, 1),
            Season = season,
            SeasonPt = SeasonNames.TryGetValue(season, out string? pt) ? pt : season,
            IsRaining = SafeGet(static () => Game1.IsRainingHere(), false),
            WeatherToday = DescribeWeatherToday(),
            WeatherTomorrow = TranslateWeather(SafeGet(() => Game1.weatherForTomorrow, "?")),
            DailyLuck = SafeGet(static () => Game1.player?.DailyLuck ?? 0d, 0d),
            Inventory = ReadInventory(),
            StagedItems = ReadStagingChest(config, monitor),
            HasStagingChest = config.TemBauMarcado,
            Bundles = BundleReader.ReadAll(monitor),
            RoomRewards = roomRewards,
            Farmers = ReadFarmers()
        };
    }

    private static string DescribeWeatherToday()
    {
        try
        {
            if (Game1.isSnowing) return "Neve";
            if (Game1.isLightning) return "Tempestade";
            if (Game1.IsRainingHere()) return "Chuva";
            if (Game1.isDebrisWeather) return "Vento";
            return "Sol";
        }
        catch
        {
            return "?";
        }
    }

    private static string TranslateWeather(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "?";

        return WeatherNames.TryGetValue(raw, out string? pt) ? pt : raw;
    }

    private static IReadOnlyList<Item> ReadInventory()
    {
        try
        {
            return Game1.player?.Items?.Where(static i => i != null).ToList() ?? new List<Item>();
        }
        catch
        {
            return Array.Empty<Item>();
        }
    }

    private static IReadOnlyList<Farmer> ReadFarmers()
    {
        try
        {
            return Game1.getAllFarmers()?.Where(static f => f != null).ToList() ?? new List<Farmer>();
        }
        catch
        {
            return Array.Empty<Farmer>();
        }
    }

    /// <summary>Le o bau marcado como bau do Centro Comunitario. Somente leitura.</summary>
    private static IReadOnlyList<Item> ReadStagingChest(ModConfig config, IMonitor monitor)
    {
        if (!config.TemBauMarcado)
            return Array.Empty<Item>();

        try
        {
            Chest? chest = FindStagingChest(config);
            if (chest is null)
                return Array.Empty<Item>();

            return chest.Items.Where(static i => i != null).ToList();
        }
        catch (Exception ex)
        {
            monitor.Log($"Nao consegui ler o bau marcado: {ex.Message}", LogLevel.Trace);
            return Array.Empty<Item>();
        }
    }

    /// <summary>Acha o bau marcado, ou null se ele foi movido/removido.</summary>
    public static Chest? FindStagingChest(ModConfig config)
    {
        if (!config.TemBauMarcado)
            return null;

        try
        {
            GameLocation? location = Game1.getLocationFromName(config.BauLocal);
            if (location is null)
                return null;

            var tile = new Vector2(config.BauX, config.BauY);
            if (location.objects.TryGetValue(tile, out StardewValley.Object? obj) && obj is Chest chest)
                return chest;
        }
        catch
        {
            // Bau sumiu ou localizacao nao carregada. Trata como sem bau.
        }

        return null;
    }

    private static T SafeGet<T>(Func<T> get, T fallback)
    {
        try
        {
            return get();
        }
        catch
        {
            return fallback;
        }
    }
}
