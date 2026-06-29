using Comillas.AITradingSimulator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Comillas.AITradingSimulator.Infrastructure.Persistence.Configurations;

internal sealed class MarketTickConfiguration : IEntityTypeConfiguration<MarketTick>
{
    public void Configure(EntityTypeBuilder<MarketTick> builder)
    {
        builder.ToTable("MarketTick");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Symbol)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(t => t.Price).HasConversion<string>();
        builder.Property(t => t.Volume).HasConversion<string>();

        builder.HasIndex(t => new { t.Symbol, t.Timestamp });
    }
}
