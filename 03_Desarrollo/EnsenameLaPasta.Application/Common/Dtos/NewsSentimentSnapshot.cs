namespace EnsenameLaPasta.Application.Common.Dtos;

/// <summary>Sentimiento agregado de un símbolo para un día UTC concreto (HV-056).</summary>
public sealed record NewsSentimentSnapshotDto(
    string Symbol,
    DateTime DateUtc,
    decimal Score,
    int SampleCount);
