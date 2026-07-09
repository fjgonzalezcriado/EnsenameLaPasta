using EnsenameLaPasta.Application.Common.Interfaces;
using EnsenameLaPasta.Application.Common.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnsenameLaPasta.Infrastructure.MarketData;

/// <summary>
/// Estado del proveedor activo, conmutable en runtime y persistido en un fichero
/// (<c>App_Data/active-provider.txt</c>) para sobrevivir a reinicios. Inicializa desde
/// el fichero si existe; si no, desde <c>MarketData:ProviderType</c>.
/// </summary>
public sealed class MarketProviderState : IMarketProviderState
{
    private static readonly string[] AvailableProviders = ["YahooFinance", "TwelveData", "AlphaVantage"];
    private static readonly string FilePath = Path.Combine("App_Data", "active-provider.txt");

    private readonly ILogger<MarketProviderState> _logger;
    private readonly Lock _gate = new();
    private string _current;

    public MarketProviderState(IOptions<MarketDataOptions> options, ILogger<MarketProviderState> logger)
    {
        _logger = logger;
        var persisted = TryReadFile();
        _current = Normalize(persisted ?? options.Value.ProviderType);
    }

    public string Current => _current;

    public IReadOnlyList<string> Available => AvailableProviders;

    public void Set(string provider)
    {
        var normalized = Normalize(provider);
        lock (_gate)
        {
            _current = normalized;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                File.WriteAllText(FilePath, normalized);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo persistir el proveedor activo en {File}.", FilePath);
            }
        }
        if (_logger.IsEnabled(LogLevel.Information))
            _logger.LogInformation("Proveedor de datos activo cambiado a {Provider}.", normalized);
    }

    private static string Normalize(string? provider)
        => AvailableProviders.FirstOrDefault(a => string.Equals(a, provider?.Trim(), StringComparison.OrdinalIgnoreCase))
           ?? AvailableProviders[0];

    private string? TryReadFile()
    {
        try
        {
            return File.Exists(FilePath) ? File.ReadAllText(FilePath).Trim() : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo leer el proveedor activo de {File}.", FilePath);
            return null;
        }
    }
}
