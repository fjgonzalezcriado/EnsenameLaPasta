using EnsenameLaPasta.Application.Common.Interfaces;
using EnsenameLaPasta.Domain.Entities;
using EnsenameLaPasta.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EnsenameLaPasta.Application.Services;

public sealed class OrderService(ITradingDbContext db) : IOrderService
{
    private readonly ITradingDbContext _db = db;

    public async Task<Trade?> BuyAsync(string symbol, decimal entryPrice, decimal quantity, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var sym = symbol.ToUpperInvariant();

        var hasOpen = await _db.Trades.AnyAsync(t => t.Symbol == sym && t.Status == TradeStatus.Open, cancellationToken);
        if (hasOpen) return null;

        var trade = Trade.Open(sym, entryPrice, quantity, nowUtc);
        _db.Trades.Add(trade);
        await _db.SaveChangesAsync(cancellationToken);
        return trade;
    }

    public async Task<Trade?> CloseAsync(string symbol, decimal exitPrice, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        var sym = symbol.ToUpperInvariant();

        var openTrade = await _db.Trades
            .Where(t => t.Symbol == sym && t.Status == TradeStatus.Open)
            .OrderBy(t => t.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (openTrade is null) return null;

        openTrade.Close(exitPrice, nowUtc);
        await _db.SaveChangesAsync(cancellationToken);
        return openTrade;
    }
}
