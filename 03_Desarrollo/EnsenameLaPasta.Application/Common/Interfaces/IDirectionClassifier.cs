using EnsenameLaPasta.Application.Common.Dtos;

namespace EnsenameLaPasta.Application.Common.Interfaces;

/// <summary>
/// Clasificador de la dirección del próximo periodo (sube/baja) a partir de features técnicas
/// del histórico del símbolo (HV-044). Implementado con ML.NET (regresión logística SDCA) en
/// Infrastructure. No lanza: si no hay datos suficientes, devuelve HasPrediction=false.
/// </summary>
public interface IDirectionClassifier
{
    Task<DirectionSignal> ClassifyAsync(string symbol, string range, CancellationToken cancellationToken = default);
}
