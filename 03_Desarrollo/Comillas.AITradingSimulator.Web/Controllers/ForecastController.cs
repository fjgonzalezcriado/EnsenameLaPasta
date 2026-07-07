using Comillas.AITradingSimulator.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Comillas.AITradingSimulator.Web.Controllers;

/// <summary>
/// Pronóstico de precio (ML.NET SSA) para señales del panel (HV-043).
/// </summary>
public sealed class ForecastController(IPriceForecaster forecaster) : Controller
{
    private readonly IPriceForecaster _forecaster = forecaster;

    // GET /api/forecast?symbol=IBM&range=1M&horizon=10
    [HttpGet("/api/forecast")]
    public async Task<IActionResult> Forecast(string symbol, string range, int horizon = 10, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(symbol) || string.IsNullOrWhiteSpace(range))
            return BadRequest("symbol y range son obligatorios.");

        // Acota el horizonte a un rango sano (evita peticiones abusivas).
        horizon = Math.Clamp(horizon, 1, 60);
        var forecast = await _forecaster.ForecastAsync(symbol, range, horizon, cancellationToken);
        return Json(forecast);
    }
}
