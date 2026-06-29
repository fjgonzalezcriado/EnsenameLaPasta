using Comillas.AITradingSimulator.Application.Common.Dtos;
using Comillas.AITradingSimulator.Application.Common.Interfaces;
using Comillas.AITradingSimulator.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Comillas.AITradingSimulator.Application.Services;

public sealed class WatchlistService : IWatchlistService
{
    private readonly ITradingDbContext _db;
    private readonly TimeProvider _time;

    public WatchlistService(ITradingDbContext db, TimeProvider time)
    {
        _db = db;
        _time = time;
    }

    public async Task<IReadOnlyList<TrackedSymbolDto>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _db.TrackedSymbols
            .OrderBy(t => t.Symbol)
            .Select(t => new TrackedSymbolDto(t.Symbol, t.Name, t.AddedAt, t.Currency))
            .ToListAsync(cancellationToken);

    public async Task<bool> AddAsync(string symbol, string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol es obligatorio.", nameof(symbol));

        var normalized = symbol.Trim().ToUpperInvariant();
        var exists = await _db.TrackedSymbols.AnyAsync(t => t.Symbol == normalized, cancellationToken);
        if (exists) return false;

        _db.TrackedSymbols.Add(TrackedSymbol.Create(normalized, name ?? string.Empty, _time.GetUtcNow().UtcDateTime));
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> RemoveAsync(string symbol, CancellationToken cancellationToken = default)
    {
        var normalized = (symbol ?? string.Empty).Trim().ToUpperInvariant();
        var entity = await _db.TrackedSymbols.FirstOrDefaultAsync(t => t.Symbol == normalized, cancellationToken);
        if (entity is null) return false;

        _db.TrackedSymbols.Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);

        // Borra el histórico de ticks del símbolo para que desaparezca del panel
        // (el selector se alimenta de los símbolos presentes en MarketTick).
        await _db.MarketTicks.Where(t => t.Symbol == normalized).ExecuteDeleteAsync(cancellationToken);
        return true;
    }
}
