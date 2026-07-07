using Comillas.AITradingSimulator.Application.Common.Dtos;
using Comillas.AITradingSimulator.Application.Common.Interfaces;
using Comillas.AITradingSimulator.Infrastructure.MarketData;
using Microsoft.Extensions.Logging.Abstractions;

namespace Comillas.AITradingSimulator.Tests.Infrastructure;

public class SsaPriceForecasterTests
{
    private static readonly DateTime BaseTime = new(2026, 7, 7, 9, 0, 0, DateTimeKind.Utc);

    // Serie sintética: precio = start + i*step.
    private static List<PricePoint> Series(int n, decimal start, decimal step)
    {
        var list = new List<PricePoint>(n);
        for (var i = 0; i < n; i++)
        {
            var price = start + step * i;
            list.Add(new PricePoint(BaseTime.AddMinutes(i), price, 100, price, price, price));
        }
        return list;
    }

    private static SsaPriceForecaster NewForecaster(IReadOnlyList<PricePoint> series)
        => new(new FakeHistory(series), NullLogger<SsaPriceForecaster>.Instance);

    [Fact]
    public async Task ForecastAsync_SerieSuficiente_DevuelvePronosticoConHorizonteYBanda()
    {
        var forecaster = NewForecaster(Series(40, 100m, 1m));   // 100 → 139 ascendente

        var f = await forecaster.ForecastAsync("TEST", "1M", horizon: 8);

        Assert.True(f.HasForecast);
        Assert.Equal(8, f.Points.Count);
        Assert.Equal(139m, f.LastPrice);
        // Banda coherente: inferior ≤ superior en cada punto.
        Assert.All(f.Points, p => Assert.True(p.LowerBound <= p.UpperBound));
    }

    [Fact]
    public async Task ForecastAsync_TendenciaAlcista_SenalAlcista()
    {
        var forecaster = NewForecaster(Series(48, 100m, 2m));   // fuerte pendiente positiva

        var f = await forecaster.ForecastAsync("TEST", "1M", horizon: 10);

        Assert.True(f.HasForecast);
        Assert.True(f.ForecastEnd > f.LastPrice, $"esperado alcista; end={f.ForecastEnd} last={f.LastPrice}");
        Assert.Equal("Alcista", f.Signal);
    }

    [Fact]
    public async Task ForecastAsync_HistoricoInsuficiente_NoPronostica()
    {
        var forecaster = NewForecaster(Series(5, 100m, 1m));   // < mínimo (12)

        var f = await forecaster.ForecastAsync("TEST", "1M", horizon: 10);

        Assert.False(f.HasForecast);
        Assert.Equal("Insuficiente", f.Signal);
        Assert.Empty(f.Points);
    }

    [Fact]
    public async Task ForecastAsync_HorizonteSeRespeta()
    {
        var forecaster = NewForecaster(Series(36, 50m, 0.5m));

        var f = await forecaster.ForecastAsync("TEST", "5D", horizon: 5);

        Assert.True(f.HasForecast);
        Assert.Equal(5, f.Points.Count);
    }

    private sealed class FakeHistory(IReadOnlyList<PricePoint> points) : IMarketHistoryProvider
    {
        private readonly IReadOnlyList<PricePoint> _points = points;

        public Task<IReadOnlyList<PricePoint>> GetHistoryAsync(string symbol, string range, CancellationToken cancellationToken = default)
            => Task.FromResult(_points);
    }
}
