using Microsoft.Xna.Framework;
using Pelican.Config;
using Pelican.Model;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;

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
            HeldItem = SafeGet(static () => Game1.player?.CurrentItem, null),
            HeldItemDonatable = IsDonatable(SafeGet(static () => Game1.player?.CurrentItem, null)),
            Crops = ReadCrops(season, monitor),
            StagedItems = ReadStagingChest(config, monitor),
            HasStagingChest = config.TemBauMarcado,
            Bundles = BundleReader.ReadAll(monitor),
            RoomRewards = roomRewards,
            Farmers = ReadFarmers(),
            FishCaught = ReadFishCaught(),
            MuseumDonated = ReadMuseumCount()
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

    /// <summary>Peixes ja pescados, guardados com e sem qualificador.</summary>
    private static IReadOnlySet<string> ReadFishCaught()
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            foreach (string key in Game1.player.fishCaught.Keys)
            {
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                result.Add(key);

                int close = key.IndexOf(')');
                if (close >= 0)
                    result.Add(key[(close + 1)..]);
            }
        }
        catch
        {
            // Save nao carregado ou API mudou: trata como "nao pescou nada",
            // que so causa um aviso a mais, nunca um errado.
        }

        return result;
    }

    private static int ReadMuseumCount()
    {
        try
        {
            return Game1.getLocationFromName("ArchaeologyHouse") is LibraryMuseum museum
                ? museum.museumPieces.Count()
                : 0;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// O museu ainda aceita este item? Quem decide e o proprio jogo, nao uma
    /// lista curada - assim minerais e artefatos novos da 1.7 ja entram sozinhos.
    /// </summary>
    private static bool IsDonatable(Item? item)
    {
        if (item is null)
            return false;

        try
        {
            return Game1.getLocationFromName("ArchaeologyHouse") is LibraryMuseum museum
                && museum.isItemSuitableForDonation(item);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Percorre as plantacoes da fazenda. So a Farm: estufa e ilha nao perdem
    /// cultura na virada de estacao, entao o aviso nao se aplica a elas.
    /// </summary>
    private static CropSummary ReadCrops(string season, IMonitor monitor)
    {
        try
        {
            int daysLeftToGrow = Math.Max(0, 28 - Game1.dayOfMonth);
            Season next = NextSeason(season);

            int ready = 0;
            int readyTomorrow = 0;
            var wontMature = new Dictionary<string, int>();

            foreach (GameLocation location in Game1.locations)
            {
                if (location is not Farm)
                    continue;

                foreach (var pair in location.terrainFeatures.Pairs)
                {
                    if (pair.Value is not HoeDirt dirt || dirt.crop is null)
                        continue;

                    if (dirt.readyForHarvest())
                    {
                        ready++;
                        continue;
                    }

                    int remaining = DaysUntilHarvest(dirt.crop);
                    if (remaining == 1)
                        readyTomorrow++;

                    // Vai morrer na virada? So conta se a cultura nao sobrevive
                    // para a proxima estacao.
                    if (remaining > daysLeftToGrow && !SurvivesInto(dirt.crop, next))
                    {
                        string name = CropName(dirt.crop);
                        wontMature[name] = wontMature.GetValueOrDefault(name) + 1;
                    }
                }
            }

            return new CropSummary
            {
                ReadyToHarvest = ready,
                ReadyTomorrow = readyTomorrow,
                WontMature = wontMature
            };
        }
        catch (Exception ex)
        {
            monitor.Log($"Nao consegui ler as plantacoes: {ex.Message}", LogLevel.Trace);
            return new CropSummary();
        }
    }

    /// <summary>
    /// Dias ate a colheita. A ultima entrada de phaseDays e um sentinela gigante
    /// (99999), por isso o laco para antes dela.
    /// </summary>
    private static int DaysUntilHarvest(Crop crop)
    {
        try
        {
            if (crop.fullyGrown.Value)
                return 0;

            var phases = crop.phaseDays;
            if (phases is null || phases.Count == 0)
                return 0;

            int remaining = 0;
            for (int i = crop.currentPhase.Value; i < phases.Count - 1; i++)
                remaining += phases[i];

            remaining -= crop.dayOfCurrentPhase.Value;

            // Valor implausivel = leitura errada; melhor nao afirmar nada.
            return remaining is < 0 or > 112 ? 0 : remaining;
        }
        catch
        {
            return 0;
        }
    }

    private static bool SurvivesInto(Crop crop, Season next)
    {
        try
        {
            return crop.GetData()?.Seasons?.Contains(next) == true;
        }
        catch
        {
            // Na duvida, assume que sobrevive: um alarme falso de "vai morrer"
            // e pior que um aviso a menos.
            return true;
        }
    }

    private static string CropName(Crop crop)
    {
        try
        {
            string? harvestId = crop.GetData()?.HarvestItemId;
            if (!string.IsNullOrWhiteSpace(harvestId))
                return ItemNames.Resolve(harvestId);
        }
        catch
        {
            // cai para o rotulo generico
        }

        return "planta";
    }

    private static Season NextSeason(string season)
    {
        return season?.ToLowerInvariant() switch
        {
            "spring" => Season.Summer,
            "summer" => Season.Fall,
            "fall" => Season.Winter,
            _ => Season.Spring
        };
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
