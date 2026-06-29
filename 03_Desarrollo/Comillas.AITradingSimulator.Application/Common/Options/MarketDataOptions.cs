namespace Comillas.AITradingSimulator.Application.Common.Options;

public sealed class MarketDataOptions
{
    public const string SectionName = "MarketData";

    /// <summary>Proveedor de datos en vivo. Actualmente "YahooFinance" (datos reales).</summary>
    public string ProviderType { get; set; } = "YahooFinance";

    /// <summary>Período entre ciclos de sondeo del proveedor real (ms).</summary>
    public int TickIntervalMs { get; set; } = 30000;
}
