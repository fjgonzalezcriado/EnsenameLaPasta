using EnsenameLaPasta.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnsenameLaPasta.Infrastructure.Persistence.Configurations;

internal sealed class NewsSentimentConfiguration : IEntityTypeConfiguration<NewsSentiment>
{
    public void Configure(EntityTypeBuilder<NewsSentiment> builder)
    {
        builder.ToTable("NewsSentiment");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Symbol)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(n => n.Score).HasConversion<string>();

        builder.Property(n => n.DateUtc).IsRequired();
        builder.Property(n => n.UpdatedAtUtc).IsRequired();

        // Una fila por símbolo y día: la captura hace upsert sobre esta clave.
        builder.HasIndex(n => new { n.Symbol, n.DateUtc }).IsUnique();
    }
}
