namespace EnsenameLaPasta.Application.Common.Interfaces;

/// <summary>
/// Estado (conmutable en runtime) del proveedor de datos de mercado activo.
/// Permite cambiar de proveedor desde la interfaz sin reiniciar. El valor inicial
/// procede de la configuración (<c>MarketData:ProviderType</c>) y se persiste al cambiarlo.
/// </summary>
public interface IMarketProviderState
{
    /// <summary>Proveedor activo (p.ej. "YahooFinance", "TwelveData").</summary>
    string Current { get; }

    /// <summary>Proveedores disponibles para elegir.</summary>
    IReadOnlyList<string> Available { get; }

    /// <summary>Fija el proveedor activo (validado contra <see cref="Available"/>) y lo persiste.</summary>
    void Set(string provider);
}
