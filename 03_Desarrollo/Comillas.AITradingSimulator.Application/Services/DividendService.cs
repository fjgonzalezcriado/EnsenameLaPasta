using Comillas.AITradingSimulator.Application.Common.Dtos;
using Comillas.AITradingSimulator.Application.Common.Interfaces;
using Comillas.AITradingSimulator.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Comillas.AITradingSimulator.Application.Services;

public sealed class DividendService(ITradingDbContext db, TimeProvider time) : IDividendService
{
    private readonly ITradingDbContext _db = db;
    private readonly TimeProvider _time = time;

    public async Task<IReadOnlyList<DividendDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = await _db.Dividends
            .OrderByDescending(d => d.ReceivedAt)
            .ToListAsync(cancellationToken);

        return items
            .Select(d => new DividendDto(d.Id, d.Symbol, d.Amount, d.Currency, d.ReceivedAt, d.Note))
            .ToList();
    }

    public async Task<Guid> AddAsync(string symbol, decimal amount, DateTime? receivedAtUtc = null, string? currency = "EUR", string? note = null, CancellationToken cancellationToken = default)
    {
        var dividend = Dividend.Create(symbol, amount, receivedAtUtc ?? _time.GetUtcNow().UtcDateTime, currency, note);
        _db.Dividends.Add(dividend);
        await _db.SaveChangesAsync(cancellationToken);
        return dividend.Id;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var dividend = await _db.Dividends.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (dividend is null)
            return false;

        _db.Dividends.Remove(dividend);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
