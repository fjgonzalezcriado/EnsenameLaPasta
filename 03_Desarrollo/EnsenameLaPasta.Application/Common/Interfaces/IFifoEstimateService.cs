using EnsenameLaPasta.Application.Common.Dtos;

namespace EnsenameLaPasta.Application.Common.Interfaces;

/// <summary>
/// Estimación de ganancia patrimonial aplicando FIFO (HV-059) sobre los lotes abiertos de un
/// símbolo, tal y como exige Hacienda al vender acciones compradas en fechas distintas.
/// </summary>
public interface IFifoEstimateService
{
    /// <summary>
    /// Simula la venta de <paramref name="quantity"/> unidades de <paramref name="symbol"/>
    /// consumiendo primero los lotes abiertos más antiguos (FIFO).
    /// </summary>
    /// <param name="symbol">Símbolo (se normaliza a mayúsculas).</param>
    /// <param name="quantity">Cantidad a vender (debe ser &gt; 0).</param>
    /// <param name="exitPrice">
    /// Precio de venta a usar. Si es <c>null</c>, se usa el último precio de mercado conocido del
    /// símbolo (o el precio de entrada del primer lote si no hay ningún tick todavía).
    /// </param>
    Task<FifoEstimateDto> EstimateAsync(
        string symbol,
        decimal quantity,
        decimal? exitPrice = null,
        CancellationToken cancellationToken = default);
}
