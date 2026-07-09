namespace EnsenameLaPasta.Application.Common.Options;

/// <summary>
/// Política de retención de la base de datos por tamaño: cuando el fichero
/// supera <see cref="MaxDatabaseSizeMb"/>, se purgan los ticks más antiguos
/// hasta bajar al <see cref="LowWaterMarkFraction"/> del límite y se compacta (VACUUM).
/// </summary>
public sealed class RetentionOptions
{
    public const string SectionName = "Retention";

    /// <summary>Activa/desactiva la purga automática.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Tamaño máximo de la BD en MB antes de purgar. Por defecto 1 GB.</summary>
    public int MaxDatabaseSizeMb { get; set; } = 1024;

    /// <summary>Cada cuántos segundos comprobar el tamaño.</summary>
    public int CheckIntervalSeconds { get; set; } = 300;

    /// <summary>
    /// Marca de agua baja: tras purgar, se deja la BD aproximadamente a esta
    /// fracción del límite (0–1). Evita purgar en cada comprobación.
    /// </summary>
    public double LowWaterMarkFraction { get; set; } = 0.8;
}
