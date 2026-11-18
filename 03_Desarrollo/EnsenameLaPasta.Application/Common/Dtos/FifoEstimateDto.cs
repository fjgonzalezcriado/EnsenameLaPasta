namespace EnsenameLaPasta.Application.Common.Dtos;

/// <summary>
/// Un lote (compra) consumido al aplicar FIFO sobre la cantidad solicitada (HV-059).
/// </summary>
/// <param name="TradeId">Id del <c>Trade</c> abierto del que se ha tomado la cantidad.</param>
/// <param name="PurchasedAt">Fecha de compra del lote (orden FIFO).</param>
/// <param name="QuantityUsed">Cantidad tomada de este lote para la venta simulada.</param>
/// <param name="EntryPrice">Precio de compra del lote, en la divisa del instrumento.</param>
/// <param name="CostBasis">Coste de adquisición de <see cref="QuantityUsed"/>: precio × cantidad + comisión de compra prorrateada.</param>
/// <param name="Proceeds">Importe obtenido al precio de venta usado: <see cref="QuantityUsed"/> × precio de venta.</param>
/// <param name="Gain">Ganancia/pérdida bruta del lote (<see cref="Proceeds"/> − <see cref="CostBasis"/>), sin la comisión de venta (se resta una sola vez en el total).</param>
public sealed record FifoLotConsumptionDto(
    Guid TradeId,
    DateTime PurchasedAt,
    decimal QuantityUsed,
    decimal EntryPrice,
    decimal CostBasis,
    decimal Proceeds,
    decimal Gain);

/// <summary>
/// Estimación de ganancia patrimonial aplicando la regla FIFO de Hacienda (HV-059): al vender una
/// cantidad de un símbolo con varios lotes de compra, se consumen primero los lotes más antiguos.
/// Es una estimación informativa (no asesoramiento fiscal): no aplica la regla de los 2 meses ni
/// considera otras rentas del ahorro del contribuyente.
/// </summary>
/// <param name="QuantityRequested">Cantidad que se simula vender.</param>
/// <param name="QuantityMatched">Cantidad realmente cubierta por lotes abiertos (puede ser &lt; solicitada si no hay suficiente posición).</param>
/// <param name="ExitPriceUsed">Precio de venta usado (el indicado o, por defecto, el último precio de mercado conocido).</param>
/// <param name="SellCommission">Comisión fija de venta (una sola orden, independiente del nº de lotes consumidos).</param>
/// <param name="TotalCostBasis">Suma del coste de adquisición de los lotes consumidos (incluye comisión de compra prorrateada).</param>
/// <param name="TotalProceeds">Importe total obtenido a <see cref="ExitPriceUsed"/>.</param>
/// <param name="EstimatedGain">Ganancia/pérdida patrimonial estimada neta: <see cref="TotalProceeds"/> − <see cref="TotalCostBasis"/> − <see cref="SellCommission"/>.</param>
public sealed record FifoEstimateDto(
    string Symbol,
    string Currency,
    decimal QuantityRequested,
    decimal QuantityMatched,
    decimal ExitPriceUsed,
    decimal SellCommission,
    decimal TotalCostBasis,
    decimal TotalProceeds,
    decimal EstimatedGain,
    IReadOnlyList<FifoLotConsumptionDto> Lots,
    string? Message);
