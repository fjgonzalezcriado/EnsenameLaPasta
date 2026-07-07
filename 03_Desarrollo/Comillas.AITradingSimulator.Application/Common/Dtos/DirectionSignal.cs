namespace Comillas.AITradingSimulator.Application.Common.Dtos;

/// <summary>
/// Señal de dirección del próximo periodo (clasificación binaria ML.NET, HV-044). Incluye la
/// calidad del modelo en un hold-out (accuracy/AUC) para ser honestos: predecir dirección con
/// indicadores técnicos suele rondar el azar (~50 %). Es un indicador, no asesoramiento.
/// </summary>
/// <param name="Direction">"Sube" | "Baja" (según P(sube) ≷ 0,5).</param>
/// <param name="Probability">P(sube) en [0,1] para el último dato.</param>
/// <param name="Signal">"Comprar" | "Vender" | "Mantener".</param>
/// <param name="Accuracy">Aciertos en el 20 % final (hold-out cronológico), en [0,1].</param>
/// <param name="Auc">Área bajo la curva ROC en el hold-out.</param>
public sealed record DirectionSignal(
    string Symbol,
    string Range,
    bool HasPrediction,
    string Direction,
    double Probability,
    string Signal,
    double Accuracy,
    double Auc,
    int TrainSamples,
    int FeatureCount,
    string ModelUsed,
    string? Message);
