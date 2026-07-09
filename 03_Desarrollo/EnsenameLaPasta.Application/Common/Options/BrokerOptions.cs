namespace EnsenameLaPasta.Application.Common.Options;

/// <summary>
/// Configuración del bróker (HV-050). Por defecto, Trade Republic: comisión fija de 1 € por orden
/// (Fremdkostenpauschale), sin porcentaje. Se aplica al abrir y al cerrar una posición.
/// </summary>
public sealed class BrokerOptions
{
    public const string SectionName = "Broker";

    /// <summary>Comisión fija por orden, en divisa base (Trade Republic: 1,00 €). Un round-trip = 2×.</summary>
    public decimal CommissionPerOrder { get; set; } = 1.00m;
}
