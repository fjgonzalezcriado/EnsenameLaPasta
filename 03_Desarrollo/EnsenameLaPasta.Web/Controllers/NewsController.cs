using EnsenameLaPasta.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EnsenameLaPasta.Web.Controllers;

/// <summary>
/// Noticias recientes por símbolo, combinando Yahoo Finance (HV-055) y onvista.de (HV-057) en
/// una sola lista ordenada por fecha, y el histórico de sentimiento diario acumulado en
/// background (HV-056, solo sobre Yahoo).
/// </summary>
public sealed class NewsController(INewsAggregatorService newsAggregator, INewsSentimentService sentimentService) : Controller
{
    private readonly INewsAggregatorService _newsAggregator = newsAggregator;
    private readonly INewsSentimentService _sentimentService = sentimentService;

    // GET /api/news?symbol=AAPL&max=6
    [HttpGet("/api/news")]
    public async Task<IActionResult> GetNews(string symbol, int max = 6, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            return BadRequest("symbol es obligatorio.");

        // Acota para no pedir listas desproporcionadas.
        max = Math.Clamp(max, 1, 20);
        var news = await _newsAggregator.GetNewsAsync(symbol, max, cancellationToken);
        return Json(news);
    }

    // GET /api/news/sentiment-history?symbol=AAPL
    // Histórico de sentimiento diario ya capturado (HV-056). Solo lectura: la captura la hace
    // NewsSentimentCaptureService en background, este endpoint no dispara ninguna captura.
    [HttpGet("/api/news/sentiment-history")]
    public async Task<IActionResult> GetSentimentHistory(string symbol, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            return BadRequest("symbol es obligatorio.");

        var history = await _sentimentService.GetHistoryAsync(symbol, cancellationToken);
        return Json(history);
    }
}
