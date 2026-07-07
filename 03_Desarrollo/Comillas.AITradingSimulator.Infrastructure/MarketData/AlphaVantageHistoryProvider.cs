using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Comillas.AITradingSimulator.Application.Common.Dtos;
using Comillas.AITradingSimulator.Application.Common.Interfaces;
using Comillas.AITradingSimulator.Application.Common.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Comillas.AITradingSimulator.Infrastructure.MarketData;

/// <summary>
/// Histórico OHLC+volumen desde Alpha Vantage (TIME_SERIES_INTRADAY/DAILY/WEEKLY). Tercer
/// proveedor de histórico; se resuelve en runtime cuando el proveedor activo es "AlphaVantage".
/// El nombre de la serie en la respuesta varía ("Time Series (5min)", "Time Series (Daily)",
/// "Weekly Time Series"…), así que se localiza dinámicamente.
/// </summary>
public sealed class AlphaVantageHistoryProvider : IMarketHistoryProvider
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IOptionsMonitor<AlphaVantageOptions> _options;
    private readonly TimeProvider _time;
    private readonly ILogger<AlphaVantageHistoryProvider> _logger;

    public AlphaVantageHistoryProvider(
        IHttpClientFactory httpFactory,
        IOptionsMonitor<AlphaVantageOptions> options,
        TimeProvider time,
        ILogger<AlphaVantageHistoryProvider> logger)
    {
        _httpFactory = httpFactory;
        _options = options;
        _time = time;
        _logger = logger;
    }

    private enum Kind { Intraday, Daily, Weekly }

    // Rango de la UI -> (función, intervalo intradía, outputsize, nº máx. de puntos a conservar).
    // MaxPoints=0 => sin recorte. Alpha Vantage no acepta "últimos N días"; recortamos nosotros.
    private static readonly IReadOnlyDictionary<string, (Kind Kind, string Interval, string OutputSize, int MaxPoints)> RangeMap =
        new Dictionary<string, (Kind, string, string, int)>(StringComparer.OrdinalIgnoreCase)
        {
            ["1D"] = (Kind.Intraday, "5min", "compact", 0),
            ["5D"] = (Kind.Intraday, "30min", "compact", 70),
            ["1M"] = (Kind.Daily, "", "compact", 22),
            ["3M"] = (Kind.Daily, "", "compact", 66),
            ["6M"] = (Kind.Daily, "", "full", 130),
            ["YTD"] = (Kind.Daily, "", "full", 0),   // filtrado por año en curso
            ["1A"] = (Kind.Daily, "", "full", 252),
            ["3A"] = (Kind.Weekly, "", "", 156),
            ["5A"] = (Kind.Weekly, "", "", 260),
        };

    public async Task<IReadOnlyList<PricePoint>> GetHistoryAsync(string symbol, string range, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol es obligatorio.", nameof(symbol));
        if (!RangeMap.TryGetValue(range, out var r))
            throw new ArgumentException($"Rango no soportado: '{range}'.", nameof(range));

        var apiKey = _options.CurrentValue.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            // Sin key no se puede pedir histórico; degradamos a vacío (no rompe el gráfico) con
            // un aviso de una línea, en vez de lanzar y ensuciar el log con un stack cada recarga.
            _logger.LogWarning("Alpha Vantage histórico {Symbol} {Range}: falta la API key; sin datos.", symbol, range);
            return [];
        }

        var function = r.Kind switch
        {
            Kind.Intraday => "TIME_SERIES_INTRADAY",
            Kind.Weekly => "TIME_SERIES_WEEKLY",
            _ => "TIME_SERIES_DAILY",
        };
        var url = $"/query?function={function}&symbol={Uri.EscapeDataString(symbol)}&apikey={Uri.EscapeDataString(apiKey)}";
        if (r.Kind == Kind.Intraday) url += $"&interval={r.Interval}&outputsize={r.OutputSize}";
        else if (r.Kind == Kind.Daily) url += $"&outputsize={r.OutputSize}";

        var client = _httpFactory.CreateClient(AlphaVantageProvider.HttpClientName);
        var response = await client.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Alpha Vantage histórico {Symbol} {Range}: HTTP {Code}; sin datos.",
                symbol, range, (int)response.StatusCode);
            return [];
        }

        JsonElement root;
        try
        {
            root = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
        }
        catch (JsonException)
        {
            _logger.LogWarning("Alpha Vantage histórico {Symbol} {Range}: respuesta no deserializable; sin datos.", symbol, range);
            return [];
        }
        if (root.ValueKind != JsonValueKind.Object)
            return [];

        // Nota de cuota / error → sin datos (no rompe el gráfico), con log claro.
        foreach (var name in new[] { "Note", "Information", "Error Message" })
        {
            if (root.TryGetProperty(name, out var msg) && msg.ValueKind == JsonValueKind.String)
            {
                _logger.LogWarning("Alpha Vantage histórico {Symbol} {Range}: {Message}; sin datos.", symbol, range, msg.GetString());
                return [];
            }
        }

        // Localiza la serie temporal (su clave varía según la función).
        JsonElement series = default;
        var found = false;
        foreach (var prop in root.EnumerateObject())
        {
            if (prop.Name.Contains("Time Series", StringComparison.OrdinalIgnoreCase)
                && prop.Value.ValueKind == JsonValueKind.Object)
            {
                series = prop.Value;
                found = true;
                break;
            }
        }
        if (!found) return [];

        var year = _time.GetUtcNow().UtcDateTime.Year;
        var points = new List<PricePoint>();
        foreach (var bar in series.EnumerateObject())
        {
            if (!DateTime.TryParse(bar.Name, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var ts))
                continue;
            if (range.Equals("YTD", StringComparison.OrdinalIgnoreCase) && ts.Year != year)
                continue;
            var v = bar.Value;
            var close = ParseField(v, "4. close");
            if (close <= 0) continue;
            var open = ParseField(v, "1. open"); if (open <= 0) open = close;
            var high = ParseField(v, "2. high"); if (high <= 0) high = close;
            var low = ParseField(v, "3. low"); if (low <= 0) low = close;
            var volume = ParseField(v, "5. volume");
            points.Add(new PricePoint(DateTime.SpecifyKind(ts, DateTimeKind.Utc), close, volume,
                Open: open, High: high, Low: low));
        }

        // Alpha Vantage devuelve del más reciente al más antiguo → ascendente.
        points.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));
        if (r.MaxPoints > 0 && points.Count > r.MaxPoints)
            points = points.GetRange(points.Count - r.MaxPoints, r.MaxPoints);

        _logger.LogDebug("Alpha Vantage histórico {Symbol} {Range}: {Count} puntos.", symbol, range, points.Count);
        return points;
    }

    private static decimal ParseField(JsonElement bar, string name)
        => bar.TryGetProperty(name, out var el) && el.ValueKind == JsonValueKind.String
           && decimal.TryParse(el.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var d)
            ? d : 0m;
}
