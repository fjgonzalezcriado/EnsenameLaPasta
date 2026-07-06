using Comillas.AITradingSimulator.Application.Common.Dtos;
using Comillas.AITradingSimulator.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Comillas.AITradingSimulator.Web.Controllers;

public sealed class DashboardController : Controller
{
    private readonly IDashboardService _service;
    private readonly IMarketHistoryProvider _history;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IDashboardService service, IMarketHistoryProvider history, ILogger<DashboardController> logger)
    {
        _service = service;
        _history = history;
        _logger = logger;
    }

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
        var history = await _service.GetAccountHistoryAsync(maxPoints, cancellationToken);
        return Json(history);
    }

    [HttpGet("/api/history")]
    public async Task<IActionResult> History(string symbol, string range, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(symbol) || string.IsNullOrWhiteSpace(range))
            return BadRequest("Parámetros 'symbol' y 'range' obligatorios.");

        try
        {
            var points = await _history.GetHistoryAsync(symbol, range, cancellationToken);
            return Json(new PriceSeriesDto(symbol, points));
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
