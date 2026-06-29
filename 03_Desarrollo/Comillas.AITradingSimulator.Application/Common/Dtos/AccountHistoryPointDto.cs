namespace Comillas.AITradingSimulator.Application.Common.Dtos;

/// <summary>
/// Punto del histórico del valor de cuenta (derivado de un PortfolioSnapshot).
/// </summary>
/// <param name="Timestamp">Marca temporal del snapshot (UTC).</param>
/// <param name="AccountValue">Valor de cuenta en ese instante (= efectivo + cartera).</param>
/// <param name="NetDeposits">Aportado neto derivado = AccountValue − TotalPnL.</param>
/// <param name="TotalPnL">PnL total (realizado + no realizado) en ese instante.</param>
/// <param name="ReturnPct">Rentabilidad sobre aportado = TotalPnL / NetDeposits × 100 (0 si no hay aportaciones).</param>
public sealed record AccountHistoryPointDto(
    DateTime Timestamp,
    decimal AccountValue,
    decimal NetDeposits,
    decimal TotalPnL,
    decimal ReturnPct);
