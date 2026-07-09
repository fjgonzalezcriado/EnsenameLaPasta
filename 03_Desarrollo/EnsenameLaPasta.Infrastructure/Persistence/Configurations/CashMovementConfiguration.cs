using EnsenameLaPasta.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnsenameLaPasta.Infrastructure.Persistence.Configurations;

internal sealed class CashMovementConfiguration : IEntityTypeConfiguration<CashMovement>
{
    public void Configure(EntityTypeBuilder<CashMovement> builder)
    {
        builder.ToTable("CashMovement");
        builder.HasKey(m => m.Id);

        // SQLite no tiene decimal nativo. Almacenar como TEXT preserva precisión.
        builder.Property(m => m.Amount).HasConversion<string>();

        builder.Property(m => m.Note).HasMaxLength(200);
        builder.Property(m => m.Currency).HasMaxLength(8);
        builder.Property(m => m.CreatedAt).IsRequired();

        builder.HasIndex(m => m.CreatedAt);
    }
}
