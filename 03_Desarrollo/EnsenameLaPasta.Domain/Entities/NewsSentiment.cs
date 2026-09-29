namespace EnsenameLaPasta.Domain.Entities;

/// <summary>
/// Sentimiento agregado de las noticias de un símbolo para un día UTC concreto (HV-056).
/// Una fila por (Symbol, DateUtc): se recalcula/actualiza a lo largo del día conforme llegan
/// más titulares (upsert), no se acumulan filas duplicadas. Es la base para, con histórico
/// suficiente, poder alinearla por día como feature del clasificador ML — igual que el VIX
/// (HV-052) — sin tocar el modelo todavía.
/// </summary>
public sealed class NewsSentiment
{
    public Guid Id { get; private set; }
    public string Symbol { get; private set; } = string.Empty;

    /// <summary>Día UTC (medianoche) al que corresponde el agregado.</summary>
    public DateTime DateUtc { get; private set; }

    /// <summary>Media del sentimiento de los titulares del día, en [-1, 1] (-1 muy negativo, +1 muy positivo).</summary>
    public decimal Score { get; private set; }

    /// <summary>Nº de titulares considerados en el agregado (incluye los neutros, con score 0).</summary>
    public int SampleCount { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    private NewsSentiment() { }

    public static NewsSentiment Create(string symbol, DateTime dateUtc, decimal score, int sampleCount, DateTime updatedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol es obligatorio.", nameof(symbol));
        ValidateScore(score);
        ValidateSampleCount(sampleCount);

        return new NewsSentiment
        {
            Id = Guid.NewGuid(),
            Symbol = symbol.Trim().ToUpperInvariant(),
            DateUtc = dateUtc.Date,
            Score = score,
            SampleCount = sampleCount,
            UpdatedAtUtc = updatedAtUtc
        };
    }

    /// <summary>Recalcula el agregado del día (se ejecuta cada vez que se captura de nuevo esa jornada).</summary>
    public void Update(decimal score, int sampleCount, DateTime updatedAtUtc)
    {
        ValidateScore(score);
        ValidateSampleCount(sampleCount);
        Score = score;
        SampleCount = sampleCount;
        UpdatedAtUtc = updatedAtUtc;
    }

    private static void ValidateScore(decimal score)
    {
        if (score < -1m || score > 1m)
            throw new ArgumentOutOfRangeException(nameof(score), "Score debe estar en [-1, 1].");
    }

    private static void ValidateSampleCount(int sampleCount)
    {
        if (sampleCount < 0)
            throw new ArgumentOutOfRangeException(nameof(sampleCount), "SampleCount no puede ser negativo.");
    }
}
