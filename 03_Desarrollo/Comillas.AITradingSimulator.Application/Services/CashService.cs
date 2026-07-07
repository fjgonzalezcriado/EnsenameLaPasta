using Comillas.AITradingSimulator.Application.Common.Dtos;
using Comillas.AITradingSimulator.Application.Common.Interfaces;
using Comillas.AITradingSimulator.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Comillas.AITradingSimulator.Application.Services;

public sealed class CashService(ITradingDbContext db, TimeProvider time) : ICashService
{
    private readonly ITradingDbContext _db = db;
    private readonly TimeProvider _time = time;

    public async Task<IReadOnlyList<CashMovementDto>> GetMovementsAsync(CancellationToken cancellationToken = default)
    {
        var movements = await _db.CashMovements
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

        return [.. movements.Select(m => new CashMovementDto(m.Id, m.Amount, m.Note, m.Currency, m.CreatedAt))];
    }

    public async Task<decimal> GetNetDepositsAsync(CancellationToken cancellationToken = default)
    {
        // Importe se almacena como TEXT (decimal); sumar en memoria.
        var amounts = await _db.CashMovements.Select(m => m.Amount).ToListAsync(cancellationToken);
        return amounts.Sum();
    }

    public async Task<Guid> AddAsync(decimal amount, string? note, DateTime? createdAtUtc = null, string? currency = "EUR", CancellationToken cancellationToken = default)
    {
        var movement = CashMovement.Create(amount, note, createdAtUtc ?? _time.GetUtcNow().UtcDateTime, currency);
        _db.CashMovements.Add(movement);
        await _db.SaveChangesAsync(cancellationToken);
        return movement.Id;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var movement = await _db.CashMovements.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (movement is null)
            return false;

        _db.CashMovements.Remove(movement);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
