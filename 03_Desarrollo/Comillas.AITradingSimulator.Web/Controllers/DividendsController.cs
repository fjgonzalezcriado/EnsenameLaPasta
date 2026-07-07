using Comillas.AITradingSimulator.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Comillas.AITradingSimulator.Web.Controllers;

/// <summary>
/// Dividendos cobrados por el usuario (HV-050). Suman al PnL y al efectivo.
/// </summary>
public sealed class DividendsController(IDividendService dividends) : Controller
{
    private readonly IDividendService _dividends = dividends;

    [HttpGet("/api/dividends")]
    public async Task<IActionResult> All(CancellationToken cancellationToken)
        => Json(await _dividends.GetAllAsync(cancellationToken));

    [HttpPost("/api/dividends")]
    public async Task<IActionResult> Add([FromBody] DividendRequest? request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Symbol) || request.Amount <= 0)
            return BadRequest("'symbol' obligatorio y 'amount' > 0.");

        try
        {
            var id = await _dividends.AddAsync(request.Symbol, request.Amount, request.ReceivedAt, request.Currency, request.Note, cancellationToken);
            return Json(new { id });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("/api/dividends/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var ok = await _dividends.DeleteAsync(id, cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    public sealed record DividendRequest(string Symbol, decimal Amount, DateTime? ReceivedAt, string? Currency, string? Note);
}
