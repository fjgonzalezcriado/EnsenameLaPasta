using Comillas.AITradingSimulator.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Comillas.AITradingSimulator.Web.Controllers;

/// <summary>
/// Alta/cierre/borrado de posiciones reales del usuario (tracker de cartera).
/// </summary>
public sealed class PositionsController(IPositionService positions) : Controller
{
    private readonly IPositionService _positions = positions;

    [HttpPost("/api/positions")]
    public async Task<IActionResult> Open([FromBody] OpenPositionRequest? request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Symbol))
            return BadRequest("'symbol' obligatorio.");
        if (request.EntryPrice <= 0)
            return BadRequest("'entryPrice' debe ser > 0.");
        if (request.Quantity <= 0)
            return BadRequest("'quantity' debe ser > 0.");

        try
        {
            var id = await _positions.OpenAsync(request.Symbol, request.EntryPrice, request.Quantity, request.OpenedAt, cancellationToken);
            return Json(new { id });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPost("/api/positions/{id:guid}/close")]
    public async Task<IActionResult> Close(Guid id, [FromBody] ClosePositionRequest? request, CancellationToken cancellationToken)
    {
        if (request is null || request.ExitPrice <= 0)
            return BadRequest("'exitPrice' debe ser > 0.");

        try
        {
            var ok = await _positions.CloseAsync(id, request.ExitPrice, request.ClosedAt, cancellationToken);
            return ok ? NoContent() : NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("/api/positions/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var ok = await _positions.DeleteAsync(id, cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    [HttpPost("/api/positions/import")]
    public async Task<IActionResult> Import([FromBody] ImportCsvRequest? request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Csv))
            return BadRequest("'csv' obligatorio.");

        var result = await _positions.ImportCsvAsync(request.Csv, cancellationToken);
        return Json(result);
    }

    public sealed record OpenPositionRequest(string Symbol, decimal EntryPrice, decimal Quantity, DateTime? OpenedAt);
    public sealed record ClosePositionRequest(decimal ExitPrice, DateTime? ClosedAt);
    public sealed record ImportCsvRequest(string Csv);
}
