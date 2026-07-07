namespace Comillas.AITradingSimulator.Application.Common.Dtos;

/// <summary>
/// Métricas avanzadas de la cartera (HV-048), derivadas de la curva de capital
/// (<c>PortfolioSnapshot</c>) y de los trades cerrados. Sharpe y volatilidad se calculan sobre
/// retornos diarios (último snapshot de cada día) anualizados con √252 — aproximados con pocos
/// datos; <see cref="HasEnoughData"/> indica si hay suficientes días.
/// </summary>
/// <param name="MaxDrawdownPct">Peor caída pico→valle (%), negativa.</param>
/// <param name="CurrentDrawdownPct">Caída actual desde el último máximo (%), ≤ 0.</param>
/// <param name="SharpeRatio">Ratio de Sharpe anualizado (rf = 0).</param>
/// <param name="AnnualizedVolatilityPct">Volatilidad anualizada de los retornos diarios (%).</param>
/// <param name="ProfitFactor">Σ ganancias / Σ pérdidas de trades cerrados (0 si no aplica).</param>
/// <param name="ProfitFactorInfinite">true si solo hay ganancias (sin pérdidas).</param>
public sealed record PortfolioMetricsDto(
    bool HasEnoughData,
    int SnapshotCount,
    int DailyPoints,
    decimal PeakValue,
    decimal CurrentValue,
    decimal MaxDrawdownPct,
    decimal CurrentDrawdownPct,
    double SharpeRatio,
    double AnnualizedVolatilityPct,
    decimal ProfitFactor,
    bool ProfitFactorInfinite,
    int ClosedTrades,
    string? Message);
