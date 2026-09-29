using EnsenameLaPasta.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EnsenameLaPasta.Web.Controllers;

/// <summary>
/// Noticias recientes por símbolo, vía Yahoo Finance (HV-055).
/// </summary>
public sealed class NewsController(INewsProvider newsProvider) : Controller
{
    private readonly INewsProvider _newsProvider = newsProvider;

    // GET /api/news?symbol=AAPL&max=6
    [HttpGet("/api/news")]
    public async Task<IActionResult> GetNews(string symbol, int max = 6, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            return BadRequest("symbol es obligatorio.");

        // Acota para no pedir listas desproporcionadas.
        max = Math.Clamp(max, 1, 20);
        var news = await _newsProvider.GetNewsAsync(symbol, max, cancellationToken);
        return Json(news);
    }
}
