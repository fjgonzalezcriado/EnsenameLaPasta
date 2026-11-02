namespace EnsenameLaPasta.Application.Common.Interfaces;

/// <summary>
/// Cierre del día de negociación anterior para un símbolo. Lo necesita el rango "1D" del
/// gráfico de precios para calcular la variación acumulada igual que hace cualquier bolsa real
/// (precio actual vs. cierre de ayer, no vs. la apertura de hoy). Capacidad separada de
/// <see cref="IMarketHistoryProvider"/> porque no todos los proveedores la soportan: una
/// implementación sin este dato puede no registrarse, y el consumidor lo trata como opcional.
/// </summary>
public interface IPreviousCloseProvider
{
    /// <summary>
    /// Devuelve el cierre anterior, o null si no está disponible (proveedor sin este dato,
    /// símbolo inválido, fuente caída). No lanza.
    /// </summary>
    Task<decimal?> GetPreviousCloseAsync(string symbol, CancellationToken cancellationToken = default);
}
