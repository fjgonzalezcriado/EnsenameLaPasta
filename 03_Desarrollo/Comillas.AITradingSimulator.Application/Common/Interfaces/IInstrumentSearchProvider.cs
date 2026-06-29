using Comillas.AITradingSimulator.Application.Common.Dtos;

namespace Comillas.AITradingSimulator.Application.Common.Interfaces;

/// <summary>
/// Busca instrumentos financieros por descripción, ISIN o ticker/core.
/// </summary>
public interface IInstrumentSearchProvider
{
    /// <summary>
    /// Devuelve los instrumentos que coinciden con <paramref name="query"/>
    /// (nombre, ISIN o ticker). Lista vacía si no hay coincidencias.
    /// </summary>
    Task<IReadOnlyList<InstrumentSearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default);
}
