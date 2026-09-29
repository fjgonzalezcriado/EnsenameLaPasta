using EnsenameLaPasta.Application.Services;

namespace EnsenameLaPasta.Tests.Application;

public class KeywordSentimentScorerTests
{
    private readonly KeywordSentimentScorer _sut = new();

    [Fact]
    public void Score_TituloPositivo_DevuelvePositivo()
    {
        var score = _sut.Score("Apple stock surges after record profit");

        Assert.Equal(1m, score);
    }

    [Fact]
    public void Score_TituloNegativo_DevuelveNegativo()
    {
        var score = _sut.Score("Company shares plunge after massive losses");

        Assert.Equal(-1m, score);
    }

    [Fact]
    public void Score_TituloNeutro_DevuelveCero()
    {
        var score = _sut.Score("Apple releases new iPhone colors");

        Assert.Equal(0m, score);
    }

    [Fact]
    public void Score_TituloMixto_DevuelveProporcional()
    {
        var score = _sut.Score("Stock surges then plunges on mixed earnings");

        Assert.Equal(0m, score);   // 1 positiva (surges) + 1 negativa (plunges) -> (1-1)/2
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Score_TextoVacio_DevuelveCero(string? text)
    {
        Assert.Equal(0m, _sut.Score(text!));
    }

    [Fact]
    public void Score_EsCaseInsensitive()
    {
        Assert.Equal(_sut.Score("surge"), _sut.Score("SURGE"));
        Assert.True(_sut.Score("SURGE") > 0m);
    }

    [Fact]
    public void Score_NoHaceMatchParcialDePalabras()
    {
        // "dismisses" contiene "miss" como subcadena, pero no es la palabra "miss":
        // el límite de palabra (\b) no debe dar un falso positivo negativo aquí.
        var score = _sut.Score("Company dismisses analyst rumors");

        Assert.Equal(0m, score);
    }

    [Fact]
    public void Score_MasPositivasQueNegativas_QuedaEntreCeroYUno()
    {
        // 2 positivas (gains, strong) + 1 negativa (weak) -> (2-1)/3
        var score = _sut.Score("Stock gains on strong demand despite weak guidance");

        Assert.Equal(1m / 3m, score);
    }
}
