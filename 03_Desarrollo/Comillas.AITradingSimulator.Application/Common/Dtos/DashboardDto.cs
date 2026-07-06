namespace Comillas.AITradingSimulator.Application.Common.Dtos;

public sealed class DashboardDto
{
    /// <summary>Coste base de las posiciones abiertas (Σ EntryPrice × Quantity).</summary>
    public decimal Invested { get; init; }

    /// <summary>Valor de mercado de las posiciones abiertas a precio real (Σ CurrentPrice × Quantity).</summary>
    public decimal MarketValue { get; init; }

    /// <summary>Efectivo aportado neto (ingresos − retiradas).</summary>
    public decimal NetDeposits { get; init; }

    /// <summary>Efectivo disponible = aportado − invertido en abiertas + PnL realizado.</summary>
    public decimal Cash { get; init; }

    /// <summary>Valor de cuenta = efectivo + valor de mercado de las posiciones abiertas.</summary>
    public decimal AccountValue { get; init; }

    /// <summary>Divisa base en la que se expresan los totales (ISO, p.ej. "EUR").</summary>
    public string BaseCurrency { get; init; } = "EUR";

    /// <summary>Rentabilidad de la cuenta en % sobre el aportado neto = PnL total / aportado × 100 (0 si no hay aportaciones).</summary>
    public decimal ReturnPct { get; init; }

    public decimal RealizedPnL { get; init; }
    public decimal UnrealizedPnL { get; init; }
    public decimal TotalPnL { get; init; }
    public int OpenPositions { get; init; }
    public int ClosedTrades { get; init; }
    public decimal Winrate { get; init; }
    public IReadOnlyList<OpenTradeDto> OpenTrades { get; init; } = [];
    public IReadOnlyList<ClosedTradeDto> RecentClosedTrades { get; init; } = [];
    public IReadOnlyList<PriceSeriesDto> PriceSeries { get; init; } = [];

    /// <summary>Proveedor de datos activo (RandomWalk | YahooFinance).</summary>
    public string ProviderType { get; init; } = "RandomWalk";

    /// <summary>Número total de ticks de mercado persistidos.</summary>
    public int TotalTicks { get; init; }

    /// <summary>Marca temporal del último tick (UTC), o null si no hay datos.</summary>
    public DateTime? LastTickUtc { get; init; }

    /// <summary>Tamaño actual de la base de datos en bytes.</summary>
    public long DatabaseSizeBytes { get; init; }
}

public sealed record OpenTradeDto(
    Guid Id,
    string Symbol,
    decimal EntryPrice,
    decimal CurrentPrice,
    decimal Quantity,
    decimal UnrealizedPnL,
    decimal ReturnPct,
    string Currency,
    decimal UnrealizedPnLBase,
    DateTime CreatedAt);

public sealed record ClosedTradeDto(
    Guid Id,
    string Symbol,
    decimal EntryPrice,
    decimal ExitPrice,
    decimal Quantity,
    decimal RealizedPnL,
    decimal ReturnPct,
    string Currency,
    decimal RealizedPnLBase,
    DateTime CreatedAt,
    DateTime ClosedAt);

public sealed record PriceSeriesDto(string Symbol, IReadOnlyList<PricePoint> Points);

public sealed record PricePoint(DateTime Timestamp, decimal Price, decimal Volume = 0);
