using EnsenameLaPasta.Application.Common.Dtos;

namespace EnsenameLaPasta.Application.Common.Interfaces;

/// <summary>
/// Captura y consulta el sentimiento diario agregado de las noticias de un símbolo (HV-056).
/// Sienta la base de histórico para, más adelante, poder usarlo como feature del clasificador
/// ML (alineado por día, igual que el VIX en HV-052); de momento solo se acumula y se puede
/// consultar, sin tocar el modelo.
/// </summary>
public interface INewsSentimentService
{
    /// <summary>
    /// Pide las noticias actuales del símbolo, puntúa cada titular y persiste (upsert) el
    /// agregado del día UTC de hoy. Idempotente: llamar varias veces el mismo día actualiza
    /// la misma fila en vez de duplicarla. Devuelve null si no hay noticias que agregar.
    /// </summary>
    Task<NewsSentimentSnapshotDto?> CaptureTodayAsync(string symbol, CancellationToken cancellationToken = default);

    /// <summary>Histórico ya persistido para un símbolo, ordenado por fecha ascendente.</summary>
    Task<IReadOnlyList<NewsSentimentSnapshotDto>> GetHistoryAsync(string symbol, CancellationToken cancellationToken = default);
}
