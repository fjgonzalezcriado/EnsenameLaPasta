namespace EnsenameLaPasta.Application.Common.Dtos;

/// <summary>Dividendo cobrado por un instrumento (HV-050). Suma al PnL y al efectivo.</summary>
public sealed record DividendDto(Guid Id, string Symbol, decimal Amount, string Currency, DateTime ReceivedAt, string Note);
