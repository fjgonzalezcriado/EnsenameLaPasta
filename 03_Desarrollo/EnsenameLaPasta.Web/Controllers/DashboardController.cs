using EnsenameLaPasta.Application.Common.Dtos;
using EnsenameLaPasta.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EnsenameLaPasta.Web.Controllers;

public sealed class DashboardController(
    IDashboardService service,
    IMarketHistoryProvider history,
    IPreviousCloseProvider previousClose,
    ILogger<DashboardController> logger) : Controller
{
    private readonly IDashboardService _service = service;
    private readonly IMarketHistoryProvider _history = history;
    private readonly IPreviousCloseProvider _previousClose = previousClose;
    private readonly ILogger<DashboardController> _logger = logger;

    [HttpGet]
    public IActionResult Index() => View();

    [HttpGet("/api/dashboard/data")]
    public async Task<IActionResult> Data(int points = 50, CancellationToken cancellationToken = default)
    {
        // Cuántos puntos de histórico por símbolo mostrar (acotado para proteger el payload).
        var pricePoints = Math.Clamp(points, 10, 5000);
        var snapshot = await _service.GetSnapshotAsync(priceSeriesPoints: pricePoints, cancellationToken: cancellationToken);
        return Json(snapshot);
    }

    [HttpGet("/api/account/history")]
    public async Task<IActionResult> AccountHistory(int points = 500, CancellationToken cancellationToken = default)
    {
        var maxPoints = Math.Clamp(points, 10, 5000);
        var accountHistory = await _service.GetAccountHistoryAsync(maxPoints, cancellationToken);
        return Json(accountHistory);
    }

    // Métricas avanzadas de cartera (drawdown, Sharpe, volatilidad, profit factor) (HV-048).
    [HttpGet("/api/account/metrics")]
    public async Task<IActionResult> Metrics(CancellationToken cancellationToken = default)
        => Json(await _service.GetPortfolioMetricsAsync(cancellationToken));

    // Desglose de trades cerrados por año/mes con PnL sumado (HV-049).
    [HttpGet("/api/account/closed-breakdown")]
    public async Task<IActionResult> ClosedBreakdown(CancellationToken cancellationToken = default)
        => Json(await _service.GetClosedTradesBreakdownAsync(cancellationToken));

    [HttpGet("/api/history")]
    public async Task<IActionResult> History(string symbol, string range, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(symbol) || string.IsNullOrWhiteSpace(range))
            return BadRequest("Parámetros 'symbol' y 'range' obligatorios.");

        try
        {
            var points = await _history.GetHistoryAsync(symbol, range, cancellationToken);

            // Variación acumulada del rango "1D" (HV-037): debe calcularse contra el cierre del
            // día anterior, no contra la apertura de hoy (HV-058). Solo aplica a 1D: para el
            // resto de rangos "primer punto vs último" dentro de la ventana SÍ es la convención
            // correcta (rentabilidad del periodo).
            decimal? dayPreviousClose = string.Equals(range, "1D", StringComparison.OrdinalIgnoreCase)
                ? await _previousClose.GetPreviousCloseAsync(symbol, cancellationToken)
                : null;

            return Json(new PriceSeriesDto(symbol, points, dayPreviousClose));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Fallo del proveedor (p.ej. símbolo no válido para el proveedor activo, red…):
            // devolvemos serie vacía para que el gráfico muestre "sin datos" sin romper.
            _logger.LogWarning(ex, "Histórico {Symbol} {Range}: fallo del proveedor; se devuelve vacío.", symbol, range);
            return Json(new PriceSeriesDto(symbol, []));
        }
    }
}
