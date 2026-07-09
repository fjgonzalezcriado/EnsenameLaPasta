using EnsenameLaPasta.Application.Common.Dtos;
using EnsenameLaPasta.Application.Common.Interfaces;
using EnsenameLaPasta.Infrastructure.MarketData;
using Microsoft.Extensions.Logging.Abstractions;

namespace EnsenameLaPasta.Tests.Infrastructure;

public class MlDirectionClassifierTests
{
    private static readonly DateTime BaseTime = new(2026, 7, 7, 9, 0, 0, DateTimeKind.Utc);
    private static readonly string[] ValidSignals = ["Comprar", "Vender", "Mantener"];
    private static readonly string[] ValidDirections = ["Sube", "Baja"];
    private static readonly string[] ValidModels = ["SDCA", "FastTree"];

    private static List<PricePoint> Series(int n, Func<int, decimal> priceFn)
    {
        var list = new List<PricePoint>(n);
        for (var i = 0; i < n; i++)
        {
            var price = priceFn(i);
            list.Add(new PricePoint(BaseTime.AddDays(i), price, 1000 + i, price, price, price));
        }
        return list;
    }

    private static MlDirectionClassifier NewClassifier(IReadOnlyList<PricePoint> series)
        => new(new FakeHistory(series), NullLogger<MlDirectionClassifier>.Instance);

    [Fact]
    public async Task ClassifyAsync_SerieMixta_DevuelvePrediccionConCalidad()
    {
        // Tendencia + oscilación → etiquetas mixtas (sube y baja), suficiente para entrenar.
        var classifier = NewClassifier(Series(80, i => 100m + (decimal)(i * 0.3) + (decimal)(4 * Math.Sin(i * 0.7))));

        var s = await classifier.ClassifyAsync("TEST", "6M");

        Assert.True(s.HasPrediction);
        Assert.InRange(s.Probability, 0.0, 1.0);
        Assert.Contains(s.Signal, ValidSignals);
        Assert.Contains(s.Direction, ValidDirections);
        Assert.Equal(s.Direction, s.Probability >= 0.5 ? "Sube" : "Baja");
        Assert.True(s.TrainSamples > 0);
        Assert.Equal(12, s.FeatureCount);
        Assert.Contains(s.ModelUsed, ValidModels);   // selección de modelo (HV-045)
        Assert.InRange(s.Accuracy, 0.0, 1.0);
        Assert.InRange(s.Auc, 0.0, 1.0);
    }

    [Fact]
    public async Task ClassifyAsync_HistoricoInsuficiente_NoPredice()
    {
        var classifier = NewClassifier(Series(20, i => 100m + i));   // < 40 puntos

        var s = await classifier.ClassifyAsync("TEST", "1M");

        Assert.False(s.HasPrediction);
        Assert.Equal("Mantener", s.Signal);
        Assert.NotNull(s.Message);
    }

    [Fact]
    public async Task ClassifyAsync_SerieMonotona_SinVariacion_NoPredice()
    {
        // Serie estrictamente creciente → todas las etiquetas "sube" → sin variación para entrenar.
        var classifier = NewClassifier(Series(60, i => 100m + i));

        var s = await classifier.ClassifyAsync("TEST", "6M");

        Assert.False(s.HasPrediction);
        Assert.NotNull(s.Message);
    }

    private sealed class FakeHistory(IReadOnlyList<PricePoint> points) : IMarketHistoryProvider
    {
        private readonly IReadOnlyList<PricePoint> _points = points;

        public Task<IReadOnlyList<PricePoint>> GetHistoryAsync(string symbol, string range, CancellationToken cancellationToken = default)
            => Task.FromResult(_points);
    }
}
