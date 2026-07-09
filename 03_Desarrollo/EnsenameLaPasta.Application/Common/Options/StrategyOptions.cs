namespace EnsenameLaPasta.Application.Common.Options;

public sealed class StrategyOptions
{
    public const string SectionName = "Strategy";

    /// <summary>Tamaño de la ventana de la MA corta.</summary>
    public int ShortWindow { get; set; } = 5;

    /// <summary>Tamaño de la ventana de la MA larga.</summary>
    public int LongWindow { get; set; } = 20;

    /// <summary>Cantidad por trade.</summary>
    public decimal Quantity { get; set; } = 1m;
}
