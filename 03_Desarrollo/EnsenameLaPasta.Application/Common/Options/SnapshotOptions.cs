namespace EnsenameLaPasta.Application.Common.Options;

/// <summary>
/// Configuración de los snapshots periódicos del valor de cuenta (histórico).
/// Cada <see cref="IntervalSeconds"/> se persiste un <see cref="Domain.Entities.PortfolioSnapshot"/>
/// con el valor de cuenta y PnL del momento, para graficar su evolución.
/// </summary>
public sealed class SnapshotOptions
{
    public const string SectionName = "Snapshot";

    /// <summary>Activa/desactiva la toma de snapshots.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Cada cuántos segundos tomar un snapshot. Por defecto 5 minutos.</summary>
    public int IntervalSeconds { get; set; } = 300;

    /// <summary>Si se toma un snapshot poco después de arrancar (para tener un punto inicial).</summary>
    public bool SnapshotOnStartup { get; set; } = true;
}
