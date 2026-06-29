using Comillas.AITradingSimulator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Comillas.AITradingSimulator.Infrastructure.Persistence.Configurations;

internal sealed class PortfolioSnapshotConfiguration : IEntityTypeConfiguration<PortfolioSnapshot>
{
    public void Configure(EntityTypeBuilder<PortfolioSnapshot> builder)
    {
        builder.ToTable("PortfolioSnapshot");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Capital).HasConversion<string>();
        builder.Property(t => t.RealizedPnL).HasConversion<string>();
        builder.Property(t => t.UnrealizedPnL).HasConversion<string>();

        builder.HasIndex(t => t.Timestamp);
    }
}
