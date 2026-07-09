using EnsenameLaPasta.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnsenameLaPasta.Infrastructure.Persistence.Configurations;

internal sealed class DividendConfiguration : IEntityTypeConfiguration<Dividend>
{
    public void Configure(EntityTypeBuilder<Dividend> builder)
    {
        builder.ToTable("Dividend");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Symbol).IsRequired().HasMaxLength(20);
        // SQLite no tiene decimal nativo. Almacenar como TEXT preserva precisión.
        builder.Property(d => d.Amount).HasConversion<string>();
        builder.Property(d => d.Currency).HasMaxLength(8);
        builder.Property(d => d.Note).HasMaxLength(200);
        builder.Property(d => d.ReceivedAt).IsRequired();

        builder.HasIndex(d => d.ReceivedAt);
        builder.HasIndex(d => d.Symbol);
    }
}
