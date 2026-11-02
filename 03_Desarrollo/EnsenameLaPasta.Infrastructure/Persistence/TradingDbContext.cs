using EnsenameLaPasta.Application.Common.Interfaces;
using EnsenameLaPasta.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnsenameLaPasta.Infrastructure.Persistence;

public sealed class TradingDbContext(DbContextOptions<TradingDbContext> options) : DbContext(options), ITradingDbContext
{
    public DbSet<Trade> Trades => Set<Trade>();
    public DbSet<MarketTick> MarketTicks => Set<MarketTick>();
    public DbSet<PortfolioSnapshot> PortfolioSnapshots => Set<PortfolioSnapshot>();
    public DbSet<TrackedSymbol> TrackedSymbols => Set<TrackedSymbol>();
    public DbSet<CashMovement> CashMovements => Set<CashMovement>();
    public DbSet<Dividend> Dividends => Set<Dividend>();
    public DbSet<NewsSentiment> NewsSentiments => Set<NewsSentiment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TradingDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
        configurationBuilder.Properties<DateTime?>().HaveConversion<NullableUtcDateTimeConverter>();
    }

    public async Task<long> GetDatabaseSizeBytesAsync(CancellationToken cancellationToken = default)
    {
        var conn = Database.GetDbConnection();
        var mustOpen = conn.State != System.Data.ConnectionState.Open;
        if (mustOpen) await conn.OpenAsync(cancellationToken);
        try
        {
            async Task<long> ScalarAsync(string pragma)
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = pragma;
                var result = await cmd.ExecuteScalarAsync(cancellationToken);
                return result is null ? 0 : Convert.ToInt64(result);
            }

            var pageCount = await ScalarAsync("PRAGMA page_count;");
            var pageSize = await ScalarAsync("PRAGMA page_size;");
            return pageCount * pageSize;
        }
        finally
        {
            if (mustOpen) await conn.CloseAsync();
        }
    }
}
