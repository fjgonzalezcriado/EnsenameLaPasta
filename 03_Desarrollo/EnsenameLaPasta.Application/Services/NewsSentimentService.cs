using EnsenameLaPasta.Application.Common.Dtos;
using EnsenameLaPasta.Application.Common.Interfaces;
using EnsenameLaPasta.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnsenameLaPasta.Application.Services;

/// <summary>
/// Implementación de <see cref="INewsSentimentService"/> (HV-056): pide noticias al
/// <see cref="INewsProvider"/> activo, las puntúa con <see cref="ISentimentScorer"/> y persiste
/// el agregado del día en <see cref="NewsSentiment"/> (upsert por símbolo+día).
/// </summary>
public sealed class NewsSentimentService(
    ITradingDbContext db,
    INewsProvider newsProvider,
    ISentimentScorer scorer,
    TimeProvider time) : INewsSentimentService
{
    // Titulares suficientes para un agregado diario razonable sin sobrecargar a Yahoo.
    private const int MaxHeadlines = 10;

    private readonly ITradingDbContext _db = db;
    private readonly INewsProvider _newsProvider = newsProvider;
    private readonly ISentimentScorer _scorer = scorer;
    private readonly TimeProvider _time = time;

    public async Task<NewsSentimentSnapshotDto?> CaptureTodayAsync(string symbol, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol es obligatorio.", nameof(symbol));

        var normalized = symbol.Trim().ToUpperInvariant();
        var news = await _newsProvider.GetNewsAsync(normalized, MaxHeadlines, cancellationToken);

        // Sin noticias hoy: no se escribe una fila a 0 muestras. El histórico queda con un
        // hueco ese día (igual que el VIX forward-fillea huecos de calendario en HV-052).
        // Mejor un hueco honesto que un 0 que parezca "sentimiento neutro real".
        if (!news.HasNews || news.Items.Count == 0)
            return null;

        var scores = news.Items.Select(i => _scorer.Score(i.Title)).ToList();
        var average = scores.Sum() / scores.Count;
        var now = _time.GetUtcNow().UtcDateTime;
        var today = now.Date;

        var existing = await _db.NewsSentiments
            .FirstOrDefaultAsync(n => n.Symbol == normalized && n.DateUtc == today, cancellationToken);

        if (existing is null)
        {
            existing = NewsSentiment.Create(normalized, today, average, scores.Count, now);
            _db.NewsSentiments.Add(existing);
        }
        else
        {
            existing.Update(average, scores.Count, now);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new NewsSentimentSnapshotDto(normalized, today, average, scores.Count);
    }

    public async Task<IReadOnlyList<NewsSentimentSnapshotDto>> GetHistoryAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var normalized = (symbol ?? string.Empty).Trim().ToUpperInvariant();
        return await _db.NewsSentiments
            .Where(n => n.Symbol == normalized)
            .OrderBy(n => n.DateUtc)
            .Select(n => new NewsSentimentSnapshotDto(n.Symbol, n.DateUtc, n.Score, n.SampleCount))
            .ToListAsync(cancellationToken);
    }
}
