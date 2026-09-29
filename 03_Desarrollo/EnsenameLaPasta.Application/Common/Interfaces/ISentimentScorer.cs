namespace EnsenameLaPasta.Application.Common.Interfaces;

/// <summary>
/// Puntúa el sentimiento de un texto (titular de noticia) en [-1, 1] (HV-056).
/// Sin dependencias externas: la implementación por defecto es un léxico de palabras clave,
/// deliberadamente simple (indicador de apoyo, no un modelo de NLP entrenado).
/// </summary>
public interface ISentimentScorer
{
    /// <summary>
    /// Puntúa <paramref name="text"/>: -1 muy negativo, +1 muy positivo, 0 neutro o sin señal
    /// (texto vacío o sin ninguna palabra del léxico).
    /// </summary>
    decimal Score(string text);
}
