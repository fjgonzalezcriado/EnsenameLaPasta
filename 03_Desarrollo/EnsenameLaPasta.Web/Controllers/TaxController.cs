using EnsenameLaPasta.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EnsenameLaPasta.Web.Controllers;

/// <summary>
/// Cálculos fiscales informativos (HV-059). No es asesoramiento fiscal.
/// </summary>
public sealed class TaxController(IFifoEstimateService fifo) : Controller
{
    private readonly IFifoEstimateService _fifo = fifo;

    /// <summary>
    /// Estimación de ganancia patrimonial FIFO al vender <paramref name="quantity"/> unidades de
    /// <paramref name="symbol"/>. Si no se indica <paramref name="exitPrice"/>, se usa el último
    /// precio de mercado conocido.
    /// </summary>
    [HttpGet("/api/tax/fifo-estimate")]
    public async Task<IActionResult> FifoEstimate(
        string symbol, decimal quantity, decimal? exitPrice, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            return BadRequest("'symbol' obligatorio.");
        if (quantity <= 0)
            return BadRequest("'quantity' debe ser > 0.");

        var result = await _fifo.EstimateAsync(symbol, quantity, exitPrice, cancellationToken);
        return Json(result);
    }
}
