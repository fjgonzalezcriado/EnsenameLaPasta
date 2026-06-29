using Comillas.AITradingSimulator.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Comillas.AITradingSimulator.Web.Controllers;

/// <summary>
/// Búsqueda de instrumentos y gestión de la watchlist (alta/baja de símbolos seguidos).
/// </summary>
public sealed class InstrumentsController : Controller
{
    private readonly IInstrumentSearchProvider _search;
    private readonly IWatchlistService _watchlist;

    public InstrumentsController(IInstrumentSearchProvider search, IWatchlistService watchlist)
    {
        _search = search;
        _watchlist = watchlist;
    }

    [HttpGet("/api/instruments/search")]
    public async Task<IActionResult> Search(string q, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
            return BadRequest("Parámetro 'q' obligatorio (mínimo 2 caracteres).");

        try
        {
            var results = await _search.SearchAsync(q.Trim(), cancellationToken);
            return Json(results);
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(StatusCodes.Status502BadGateway,
                "No se pudo contactar con el proveedor de búsqueda: " + ex.Message);
        }
    }

    [HttpGet("/api/instruments/tracked")]
    public async Task<IActionResult> Tracked(CancellationToken cancellationToken)
        => Json(await _watchlist.GetAllAsync(cancellationToken));

    [HttpPost("/api/instruments/track")]
    public async Task<IActionResult> Track([FromBody] TrackRequest? request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Symbol))
            return BadRequest("'symbol' obligatorio.");

        var added = await _watchlist.AddAsync(request.Symbol, request.Name ?? string.Empty, cancellationToken);
        return Json(new { added });
    }

    // Símbolo por query (no por ruta) porque contiene punto (p.ej. "HY9H.F"),
    // que el enrutado MVC interpretaría como extensión de archivo.
    [HttpDelete("/api/instruments/track")]
    public async Task<IActionResult> Untrack([FromQuery] string symbol, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            return BadRequest("'symbol' obligatorio.");

        var removed = await _watchlist.RemoveAsync(symbol, cancellationToken);
        return removed ? NoContent() : NotFound();
    }

    public sealed record TrackRequest(string Symbol, string? Name);
}
