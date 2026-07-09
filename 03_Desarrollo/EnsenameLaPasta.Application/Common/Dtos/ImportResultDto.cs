namespace EnsenameLaPasta.Application.Common.Dtos;

/// <summary>Resultado de importar un CSV de posiciones.</summary>
/// <param name="Imported">Número de posiciones creadas.</param>
/// <param name="Failed">Número de filas con error (no importadas).</param>
/// <param name="Errors">Detalle de errores por línea.</param>
public sealed record ImportResultDto(
    int Imported,
    int Failed,
    IReadOnlyList<ImportErrorDto> Errors);

/// <summary>Error de importación de una línea concreta del CSV.</summary>
/// <param name="Line">Número de línea (1-based, incluyendo cabecera).</param>
/// <param name="Message">Motivo del rechazo.</param>
public sealed record ImportErrorDto(int Line, string Message);
