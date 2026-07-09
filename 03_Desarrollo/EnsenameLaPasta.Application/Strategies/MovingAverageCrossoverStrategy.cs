using EnsenameLaPasta.Application.Common.Options;
using EnsenameLaPasta.Domain.Entities;
using EnsenameLaPasta.Domain.Enums;
using Microsoft.Extensions.Options;

namespace EnsenameLaPasta.Application.Strategies;

public sealed class MovingAverageCrossoverStrategy : IStrategy
{
    private readonly StrategyOptions _options;
    private readonly Dictionary<string, Queue<decimal>> _shortWindows = [];
    private readonly Dictionary<string, Queue<decimal>> _longWindows = [];

    /// <summary>Estado del último cruce conocido por símbolo. Ausente = aún sin datos suficientes.</summary>
    private readonly Dictionary<string, bool> _wasShortAbove = [];

    public MovingAverageCrossoverStrategy(IOptions<StrategyOptions> options)
    {
        _options = options.Value;
        if (_options.ShortWindow <= 0 || _options.LongWindow <= 0)
            throw new ArgumentException("Las ventanas deben ser > 0.");
        if (_options.ShortWindow >= _options.LongWindow)
            throw new ArgumentException("ShortWindow debe ser estrictamente menor que LongWindow.");
    }

    public TradeSignal? OnTick(MarketTick tick)
    {
        var symbol = tick.Symbol;

        if (!_shortWindows.TryGetValue(symbol, out var shortW))
        {
            shortW = new Queue<decimal>();
            _shortWindows[symbol] = shortW;
        }

        if (!_longWindows.TryGetValue(symbol, out var longW))
        {
            longW = new Queue<decimal>();
            _longWindows[symbol] = longW;
        }

        Enqueue(shortW, tick.Price, _options.ShortWindow);
        Enqueue(longW, tick.Price, _options.LongWindow);

        // No emitir señal hasta tener la ventana larga completa
        if (longW.Count < _options.LongWindow) return null;

        var shortMa = Average(shortW);
        var longMa = Average(longW);
        var shortAbove = shortMa > longMa;

        if (!_wasShortAbove.TryGetValue(symbol, out var previousShortAbove))
        {
            // Primer cálculo completo: registramos estado inicial, sin señal
            _wasShortAbove[symbol] = shortAbove;
            return null;
        }

        if (shortAbove == previousShortAbove)
            return null;

        // Cruce detectado
        _wasShortAbove[symbol] = shortAbove;
        return shortAbove ? TradeSignal.Buy : TradeSignal.Sell;
    }

    private static void Enqueue(Queue<decimal> queue, decimal value, int maxSize)
    {
        queue.Enqueue(value);
        while (queue.Count > maxSize) queue.Dequeue();
    }

    private static decimal Average(Queue<decimal> queue)
    {
        decimal sum = 0m;
        foreach (var v in queue) sum += v;
        return sum / queue.Count;
    }
}
