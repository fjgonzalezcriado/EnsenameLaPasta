using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EnsenameLaPasta.Infrastructure.Persistence;

// SQLite/EF Core no preserva DateTimeKind al releer una columna TEXT: todo el dominio escribe en
// UTC, pero al volver de la BD el Kind llega como Unspecified. System.Text.Json entonces serializa
// sin "Z", y el navegador trata el valor UTC como si ya fuera su hora local (sin convertir).
internal sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    v => v,
    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

internal sealed class NullableUtcDateTimeConverter() : ValueConverter<DateTime?, DateTime?>(
    v => v,
    v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);
