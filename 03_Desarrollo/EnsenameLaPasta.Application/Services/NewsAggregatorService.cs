using EnsenameLaPasta.Application.Common.Dtos;
using EnsenameLaPasta.Application.Common.Interfaces;

namespace EnsenameLaPasta.Application.Services;

/// <summary>
/// Implementación de <see cref="INewsAggregatorService"/> (HV-057): pide noticias a Yahoo
/// (<see cref="INewsProvider"/>, HV-055) y a onvista (<see cref="IOnvistaNewsProvider"/>) en
/// paralelo, combina ambas listas ordenadas por fecha descendente y recorta a <c>max</c>. Un
/// símbolo sin mapeo onvista simplemente no aporta items (HV-057/D3): el mensaje de "sin
/// noticias" solo aparece si ninguna de las dos fuentes tuvo nada.
/// </summary>
public sealed class NewsAggregatorService(
    INewsProvider yahooProvider,
    IOnvistaNewsProvider onvistaProvider) : INewsAggregatorService
{
    private readonly INewsProvider _yahooProvider = yahooProvider;
    private readonly IOnvistaNewsProvider _onvistaProvider = onvistaProvider;

    public async Task<SymbolNews> GetNewsAsync(string symbol, int max = 8, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            return new SymbolNews(symbol, false, [], "Símbolo vacío.");

        if (max < 1) max = 8;

        var yahooTask = _yahooProvider.GetNewsAsync(symbol, max, cancellationToken);
        var onvistaTask = _onvistaProvider.GetNewsAsync(symbol, max, cancellationToken);
        await Task.WhenAll(yahooTask, onvistaTask);

        var combined = yahooTask.Result.Items
            .Concat(onvistaTask.Result)
            .OrderByDescending(n => n.PublishedAtUtc)
            .Take(max)
            .ToList();

        return combined.Count == 0
            ? new SymbolNews(symbol, false, [], "Sin noticias para este símbolo.")
            : new SymbolNews(symbol, true, combined, null);
    }
}
