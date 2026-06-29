using Comillas.AITradingSimulator.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Comillas.AITradingSimulator.Web.Controllers;

/// <summary>
/// Movimientos de caja (ingresos/retiradas) del usuario.
/// </summary>
public sealed class CashController : Controller
{
    private readonly ICashService _cash;

    public CashController(ICashService cash)
    {
        _cash = cash;
    }

    [HttpGet("/api/cash/movements")]
    public async Task<IActionResult> Movements(CancellationToken cancellationToken)
        => Json(await _cash.GetMovementsAsync(cancellationToken));

    [HttpPost("/api/cash")]
    public async Task<IActionResult> Add([FromBody] CashMovementRequest? request, CancellationToken cancellationToken)
    {
        if (request is null || request.Amount == 0)
            return BadRequest("'amount' obligatorio y distinto de 0 (positivo = ingreso, negativo = retirada).");

        try
        {
            var id = await _cash.AddAsync(request.Amount, request.Note, request.CreatedAt, request.Currency, cancellationToken);
            return Json(new { id });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("/api/cash/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var ok = await _cash.DeleteAsync(id, cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    public sealed record CashMovementRequest(decimal Amount, string? Note, DateTime? CreatedAt, string? Currency);
}
