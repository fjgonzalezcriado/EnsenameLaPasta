using EnsenameLaPasta.Application.Common.Options;
using EnsenameLaPasta.Application.Strategies;
using EnsenameLaPasta.Domain.Entities;
using EnsenameLaPasta.Domain.Enums;
using Microsoft.Extensions.Options;

namespace EnsenameLaPasta.Tests.Application;

public class MovingAverageCrossoverStrategyTests
{
    private static readonly DateTime BaseTime = new(2026, 5, 26, 10, 0, 0, DateTimeKind.Utc);

    private static MovingAverageCrossoverStrategy NewStrategy(int shortW = 3, int longW = 5)
        => new(Options.Create(new StrategyOptions { ShortWindow = shortW, LongWindow = longW, Quantity = 1m }));

    private static MarketTick TickAt(int seconds, decimal price, string symbol = "AAPL")
        => MarketTick.Create(symbol, price, 100m, BaseTime.AddSeconds(seconds));

    [Theory]
    [InlineData(0, 5)]
    [InlineData(-1, 5)]
    [InlineData(5, 5)]
    [InlineData(6, 5)]
    [InlineData(5, 0)]
    public void Constructor_VentanasInvalidas_LanzaArgumentException(int sw, int lw)
    {
        var opts = Options.Create(new StrategyOptions { ShortWindow = sw, LongWindow = lw });
        Assert.Throws<ArgumentException>(() => new MovingAverageCrossoverStrategy(opts));
    }

    [Fact]
    public void OnTick_LongWindowNoLlena_RetornaNull()
    {
        var s = NewStrategy(shortW: 3, longW: 5);

        for (int i = 0; i < 4; i++)
        {
            var signal = s.OnTick(TickAt(i, 100m + i));
            Assert.Null(signal);
        }
    }

    [Fact]
    public void OnTick_PrimerDataPointConVentanasCompletas_RegistraEstadoInicialSinSenal()
    {
        var s = NewStrategy(shortW: 3, longW: 5);

        TradeSignal? lastSignal = null;
        for (int i = 0; i < 5; i++)
        {
            lastSignal = s.OnTick(TickAt(i, 100m + i));
        }

        // En el quinto tick la longWindow se llena; debería registrar estado sin señal
        Assert.Null(lastSignal);
    }

    [Fact]
    public void OnTick_CrossoverAlcista_EmiteBuy()
    {
        // Preparar histórico bajista: precios decrecientes (shortMA < longMA)
        // Luego subida fuerte: shortMA cruza por encima de longMA
        var s = NewStrategy(shortW: 3, longW: 5);

        // Llenar las ventanas con valores bajos primero
        for (int i = 0; i < 5; i++) s.OnTick(TickAt(i, 100m - i * 2));  // 100, 98, 96, 94, 92 → estado inicial: short<long o short>long?

        // Tras esos 5: short = avg(96,94,92)=94; long = avg(100,98,96,94,92)=96. short < long → estado false
        Assert.Null(s.OnTick(TickAt(5, 90m)));  // short=92, long=94. sigue false → null
        Assert.Null(s.OnTick(TickAt(6, 89m)));  // sigue bajando, sigue false

        // Subida fuerte
        TradeSignal? last = null;
        for (int i = 7; i < 15; i++)
        {
            last = s.OnTick(TickAt(i, 100m + (i - 7) * 5m));  // 100, 105, 110, ...
            if (last is not null) break;
        }

        Assert.Equal(TradeSignal.Buy, last);
    }

    [Fact]
    public void OnTick_CrossoverBajista_EmiteSell()
    {
        var s = NewStrategy(shortW: 3, longW: 5);

        // Tendencia alcista: short > long
        for (int i = 0; i < 5; i++) s.OnTick(TickAt(i, 100m + i * 2));  // 100, 102, 104, 106, 108

        // shortAvg ≈ 106, longAvg = 104 → short > long → estado inicial true
        Assert.Null(s.OnTick(TickAt(5, 110m)));   // sigue alcista
        Assert.Null(s.OnTick(TickAt(6, 112m)));

        // Caída fuerte
        TradeSignal? last = null;
        for (int i = 7; i < 15; i++)
        {
            last = s.OnTick(TickAt(i, 100m - (i - 7) * 5m));  // 100, 95, 90, ...
            if (last is not null) break;
        }

        Assert.Equal(TradeSignal.Sell, last);
    }

    [Fact]
    public void OnTick_PrecioEstable_NoEmiteSenales()
    {
        var s = NewStrategy(shortW: 3, longW: 5);

        // Precio totalmente plano → shortMA == longMA siempre → estado no cambia
        for (int i = 0; i < 20; i++)
        {
            var signal = s.OnTick(TickAt(i, 100m));
            Assert.Null(signal);
        }
    }

    [Fact]
    public void OnTick_SimbolosIndependientes_NoMezclaWindows()
    {
        var s = NewStrategy(shortW: 3, longW: 5);

        // Llenamos AAPL con tendencia alcista
        for (int i = 0; i < 10; i++) s.OnTick(TickAt(i, 100m + i, "AAPL"));

        // GOOG empieza desde cero — no debería tener señal hasta llenar SU long window
        for (int i = 0; i < 4; i++)
        {
            var signal = s.OnTick(TickAt(i, 50m, "GOOG"));
            Assert.Null(signal);  // Aún no llena (4 < 5)
        }
    }

    [Fact]
    public void OnTick_SinDosCrucesConsecutivos_NoEmiteSenal()
    {
        var s = NewStrategy(shortW: 3, longW: 5);

        // Llenar con tendencia alcista
        for (int i = 0; i < 5; i++) s.OnTick(TickAt(i, 100m + i));  // short > long, estado true

        // Continúa alcista — estado no cambia
        for (int i = 5; i < 15; i++)
        {
            var signal = s.OnTick(TickAt(i, 110m + i));
            Assert.Null(signal);  // shortAbove sigue true, sin cruce
        }
    }
}
