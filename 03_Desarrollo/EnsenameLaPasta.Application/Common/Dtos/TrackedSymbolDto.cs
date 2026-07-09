namespace EnsenameLaPasta.Application.Common.Dtos;

/// <summary>Instrumento seguido en el panel.</summary>
public sealed record TrackedSymbolDto(string Symbol, string Name, DateTime AddedAt, string Currency);
