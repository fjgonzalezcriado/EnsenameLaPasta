namespace EnsenameLaPasta.Application.Common.Interfaces;

/// <summary>
/// Proporciona tipos de cambio entre divisas (para convertir los totales a la divisa base).
/// </summary>
public interface IFxRateProvider
{
    /// <summary>
    /// Devuelve cuántas unidades de <paramref name="to"/> equivalen a 1 unidad de
    /// <paramref name="from"/>. Devuelve 1 si las divisas coinciden o alguna está vacía.
    /// Ante un fallo de la fuente, degrada al último valor conocido o a 1 (no lanza).
    /// </summary>
    Task<decimal> GetRateAsync(string from, string to, CancellationToken cancellationToken = default);

    /// <summary>
    /// Fuerza la actualización del tipo en caché (ignora el TTL). Lo usa el servicio
    /// de refresco en background para mantener los tipos calientes y sacar la llamada
    /// HTTP del hot path del dashboard. Por defecto delega en <see cref="GetRateAsync"/>.
    /// </summary>
    Task RefreshAsync(string from, string to, CancellationToken cancellationToken = default)
        => GetRateAsync(from, to, cancellationToken);
}
