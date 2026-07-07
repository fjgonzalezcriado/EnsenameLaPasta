namespace Comillas.AITradingSimulator.Application.Common.Dtos;

/// <summary>
/// Desglose por año y mes del resultado realizado (HV-049/050): PnL neto de trades cerrados
/// (bruto − comisiones) + dividendos cobrados, todo convertido a divisa base. <c>PnLBase</c>
/// incluye ya los dividendos; <c>DividendsBase</c> se expone aparte para transparencia.
/// </summary>
public sealed record ClosedTradesBreakdownDto(
    string BaseCurrency,
    decimal TotalPnLBase,
    decimal TotalDividendsBase,
    int TotalTrades,
    IReadOnlyList<YearBreakdownDto> Years);

/// <summary>Resumen anual (meses ascendente). Años del más reciente al más antiguo.</summary>
public sealed record YearBreakdownDto(
    int Year,
    decimal PnLBase,
    decimal DividendsBase,
    int Trades,
    int Wins,
    int Losses,
    IReadOnlyList<MonthBreakdownDto> Months);

/// <summary>Resumen mensual.</summary>
/// <param name="Month">Mes 1–12.</param>
public sealed record MonthBreakdownDto(
    int Month,
    decimal PnLBase,
    decimal DividendsBase,
    int Trades,
    int Wins,
    int Losses);
