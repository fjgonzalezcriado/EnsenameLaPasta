using Comillas.AITradingSimulator.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Comillas.AITradingSimulator.Application.Common.Interfaces;

public interface ITradingDbContext
{
    DbSet<Trade> Trades { get; }
    DbSet<MarketTick> MarketTicks { get; }
    DbSet<PortfolioSnapshot> PortfolioSnapshots { get; }
    DbSet<TrackedSymbol> TrackedSymbols { get; }
    DbSet<CashMovement> CashMovements { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Tamaño lógico de la BD en bytes (page_count × page_size; excluye WAL).</summary>
    Task<long> GetDatabaseSizeBytesAsync(CancellationToken cancellationToken = default);
}
