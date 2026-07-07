using Comillas.AITradingSimulator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Comillas.AITradingSimulator.Infrastructure.Persistence.Configurations;

internal sealed class TradeConfiguration : IEntityTypeConfiguration<Trade>
{
    public void Configure(EntityTypeBuilder<Trade> builder)
    {
        builder.ToTable("Trade");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Symbol)
            .IsRequired()
            .HasMaxLength(20);

        // SQLite no tiene decimal nativo. Almacenar como TEXT preserva precisión.
        builder.Property(t => t.EntryPrice).HasConversion<string>();
        builder.Property(t => t.ExitPrice).HasConversion<string?>();
        builder.Property(t => t.Quantity).HasConversion<string>();
        builder.Property(t => t.Commission).HasConversion<string>();   // HV-050 (decimal→TEXT)
        builder.Property(t => t.Status).HasConversion<int>();

        builder.HasIndex(t => new { t.Symbol, t.CreatedAt });
        builder.HasIndex(t => t.Status);
    }
}
