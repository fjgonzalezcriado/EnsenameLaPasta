namespace Comillas.AITradingSimulator.Application.Common.Dtos;

/// <summary>
/// Desglose de trades cerrados por año y mes con el PnL sumado (HV-049). El PnL se convierte a la
/// divisa base para poder sumar entre instrumentos de distintas divisas.
/// </summary>
public sealed record ClosedTradesBreakdownDto(
    string BaseCurrency,
    decimal TotalPnLBase,
    int TotalTrades,
    IReadOnlyList<YearBreakdownDto> Years);

/// <summary>Resumen anual: total y meses (ascendente). Años ordenados del más reciente al más antiguo.</summary>
public sealed record YearBreakdownDto(
    int Year,
    decimal PnLBase,
    int Trades,
    int Wins,
    int Losses,
    IReadOnlyList<MonthBreakdownDto> Months);

/// <summary>Resumen mensual.</summary>
/// <param name="Month">Mes 1–12.</param>
public sealed record MonthBreakdownDto(
    int Month,
    decimal PnLBase,
    int Trades,
    int Wins,
    int Losses);
