using EnsenameLaPasta.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EnsenameLaPasta.Web.Controllers;

/// <summary>
/// Señal de dirección (sube/baja) por clasificación ML.NET (HV-044).
/// </summary>
public sealed class SignalController(IDirectionClassifier classifier) : Controller
{
    private readonly IDirectionClassifier _classifier = classifier;

    // GET /api/signal?symbol=IBM&range=1M
    [HttpGet("/api/signal")]
    public async Task<IActionResult> Signal(string symbol, string range, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(symbol) || string.IsNullOrWhiteSpace(range))
            return BadRequest("symbol y range son obligatorios.");

        var result = await _classifier.ClassifyAsync(symbol, range, cancellationToken);
        return Json(result);
    }
}
