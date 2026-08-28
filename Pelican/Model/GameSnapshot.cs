using StardewValley;

namespace Pelican.Model;

/// <summary>
/// Foto do estado do jogo num instante. Os advisors recebem isto e so isto,
/// para que nenhum deles precise tocar em Game1 diretamente e para que o
/// custo de leitura seja pago uma vez por dia, nao uma vez por frame.
/// </summary>
public sealed class GameSnapshot
{
    public int Year { get; init; } = 1;
    public int DayOfMonth { get; init; } = 1;

    /// <summary>Chave interna do jogo: "spring", "summer", "fall", "winter".</summary>
    public string Season { get; init; } = "spring";

    public string SeasonPt { get; init; } = "Primavera";

    /// <summary>Quantos dias ainda restam nesta estacao, contando hoje.</summary>
    public int DaysLeftInSeason => Math.Max(0, 29 - this.DayOfMonth);

    public bool IsLastWeekOfSeason => this.DaysLeftInSeason <= 7;

    public bool IsRaining { get; init; }
    public string WeatherToday { get; init; } = "?";
    public string WeatherTomorrow { get; init; } = "?";

    /// <summary>-0.1 (azarado) a 0.1 (sortudo).</summary>
    public double DailyLuck { get; init; }

    public string LuckLabel => this.DailyLuck switch
    {
        >= 0.07 => "sorte otima",
        >= 0.02 => "sorte boa",
        > -0.02 => "sorte neutra",
        > -0.07 => "sorte ruim",
        _ => "sorte pessima"
    };

    /// <summary>O que voce esta carregando agora.</summary>
    public IReadOnlyList<Item> Inventory { get; init; } = Array.Empty<Item>();

    /// <summary>O que esta no bau marcado como bau do Centro Comunitario.</summary>
    public IReadOnlyList<Item> StagedItems { get; init; } = Array.Empty<Item>();

    public bool HasStagingChest { get; init; }

    public IReadOnlyList<BundleInfo> Bundles { get; init; } = Array.Empty<BundleInfo>();

    public IReadOnlyDictionary<string, RoomReward> RoomRewards { get; init; }
        = new Dictionary<string, RoomReward>();

    /// <summary>Fazendeiros na partida, para o quadro de co-op.</summary>
    public IReadOnlyList<Farmer> Farmers { get; init; } = Array.Empty<Farmer>();

    public IEnumerable<BundleInfo> IncompleteBundles => this.Bundles.Where(static b => !b.IsComplete);

    /// <summary>Recompensa da sala, quando conhecida.</summary>
    public RoomReward? RewardFor(string room)
    {
        return this.RoomRewards.TryGetValue(room, out RoomReward? reward) ? reward : null;
    }

    /// <summary>
    /// Todos os bundles da sala ja fechados, exceto este? Serve para dizer
    /// "este item fecha a sala inteira", que e o aviso de maior valor.
    /// </summary>
    public bool WouldCompleteRoom(BundleInfo bundle)
    {
        return this.Bundles
            .Where(b => b.Room == bundle.Room && b.Key != bundle.Key)
            .All(static b => b.IsComplete);
    }

    public string DateLabel => $"Ano {this.Year} · {this.SeasonPt} · Dia {this.DayOfMonth}";
}
