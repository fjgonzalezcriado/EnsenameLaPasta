using Comillas.AITradingSimulator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Comillas.AITradingSimulator.Infrastructure.Persistence.Configurations;

internal sealed class TrackedSymbolConfiguration : IEntityTypeConfiguration<TrackedSymbol>
{
    public void Configure(EntityTypeBuilder<TrackedSymbol> builder)
    {
        builder.ToTable("TrackedSymbol");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Symbol)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(t => t.Name)
            .HasMaxLength(200);

        builder.Property(t => t.Currency)
            .HasMaxLength(8);

        builder.Property(t => t.AddedAt).IsRequired();

        builder.HasIndex(t => t.Symbol).IsUnique();
    }
}
