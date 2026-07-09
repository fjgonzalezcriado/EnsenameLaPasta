using EnsenameLaPasta.Application.Common.Dtos;

namespace EnsenameLaPasta.Application.Common.Interfaces;

public interface IDashboardService
{
    Task<DashboardDto> GetSnapshotAsync(
        int priceSeriesPoints = 50,
        int recentClosedCount = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Histórico del valor de cuenta: los últimos <paramref name="maxPoints"/> snapshots
    /// ordenados ascendente por timestamp, con NetDeposits y ReturnPct derivados.
    /// </summary>
    Task<IReadOnlyList<AccountHistoryPointDto>> GetAccountHistoryAsync(
        int maxPoints = 500,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Métricas avanzadas de la cartera (HV-048): max/actual drawdown de la curva de capital,
    /// Sharpe y volatilidad anualizados (retornos diarios) y profit factor de los trades cerrados.
    /// </summary>
    Task<PortfolioMetricsDto> GetPortfolioMetricsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Desglose de trades cerrados por año/mes (según ClosedAt) con el PnL realizado sumado y
    /// convertido a divisa base (HV-049). Años del más reciente al más antiguo.
    /// </summary>
    Task<ClosedTradesBreakdownDto> GetClosedTradesBreakdownAsync(CancellationToken cancellationToken = default);
}
