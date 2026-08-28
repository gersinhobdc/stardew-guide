using StardewModdingAPI;

namespace Pelican.Data;

/// <summary>Condicoes de captura de um peixe, na medida em que deu para ler com seguranca.</summary>
public sealed record FishInfo(
    string ItemId,
    string Name,
    string Weather,
    int? StartTime,
    int? EndTime
)
{
    public bool NeedsRain => this.Weather.Equals("rainy", StringComparison.OrdinalIgnoreCase);
    public bool NeedsSun => this.Weather.Equals("sunny", StringComparison.OrdinalIgnoreCase);
    public bool AnyWeather => !this.NeedsRain && !this.NeedsSun;

    public string TimeLabel
    {
        get
        {
            if (this.StartTime is null || this.EndTime is null)
                return "";

            return $"{Format(this.StartTime.Value)}-{Format(this.EndTime.Value)}";
        }
    }

    private static string Format(int gameTime)
    {
        int hour = gameTime / 100 % 24;
        int minute = gameTime % 100;
        return $"{hour:00}:{minute:00}";
    }
}

/// <summary>
/// Le Data/Fish.
///
/// Este e o formato mais fragil do jogo: campos separados por '/' cuja ORDEM ja
/// mudou entre versoes. Por isso a leitura aqui nao confia em indice - ela
/// PROCURA os campos pelo conteudo ("rainy"/"sunny"/"both", pares de horario).
/// Se nao achar, devolve null e o advisor simplesmente nao fala daquele peixe.
/// Falhar em silencio e melhor do que afirmar horario errado.
/// </summary>
public static class FishReader
{
    private static readonly HashSet<string> WeatherTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "sunny", "rainy", "both"
    };

    public static IReadOnlyDictionary<string, FishInfo> ReadAll(IGameContentHelper content, IMonitor monitor)
    {
        var result = new Dictionary<string, FishInfo>();

        try
        {
            var raw = content.Load<Dictionary<string, string>>("Data/Fish");

            foreach ((string id, string line) in raw)
            {
                FishInfo? info = ParseOne(id, line);
                if (info != null)
                    result[id] = info;
            }
        }
        catch (Exception ex)
        {
            monitor.Log($"Nao consegui ler Data/Fish: {ex.Message}", LogLevel.Trace);
        }

        return result;
    }

    private static FishInfo? ParseOne(string id, string line)
    {
        try
        {
            string[] parts = line.Split('/');
            if (parts.Length < 2)
                return null;

            // Peixes de armadilha (crab pot) tem formato totalmente diferente; ignore.
            if (parts.Any(static p => p.Equals("trap", StringComparison.OrdinalIgnoreCase)))
                return null;

            string name = ItemNames.Resolve(id);

            // Clima: procurado por conteudo, nao por posicao.
            string weather = parts.FirstOrDefault(p => WeatherTokens.Contains(p.Trim())) ?? "both";

            // Horarios: primeiro campo que seja um par de numeros plausiveis de relogio.
            (int? start, int? end) = FindTimeRange(parts);

            return new FishInfo(id, name, weather.Trim(), start, end);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Acha o campo de horarios: pares de numeros entre 600 e 2600.</summary>
    private static (int?, int?) FindTimeRange(string[] parts)
    {
        foreach (string part in parts)
        {
            string[] tokens = part.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length < 2 || tokens.Length % 2 != 0)
                continue;

            if (!int.TryParse(tokens[0], out int start) || !int.TryParse(tokens[1], out int end))
                continue;

            bool plausible = start is >= 600 and <= 2600
                && end is >= 600 and <= 2600
                && end > start;

            if (plausible)
                return (start, end);
        }

        return (null, null);
    }
}
