using System.Text.RegularExpressions;
using EnsenameLaPasta.Application.Common.Interfaces;

namespace EnsenameLaPasta.Application.Services;

/// <summary>
/// Sentimiento por léxico de palabras clave sobre titulares de noticias financieras (HV-056).
/// Cuenta ocurrencias de palabras positivas/negativas (inglés + algo de español, los titulares
/// de Yahoo salen mayormente en inglés) y devuelve (positivas - negativas) / (positivas + negativas)
/// en [-1, 1]; 0 si el titular no contiene ninguna palabra del léxico (sin señal, no "neutro real").
/// Deliberadamente simple: es un indicador de apoyo, no un modelo de NLP entrenado.
/// </summary>
public sealed class KeywordSentimentScorer : ISentimentScorer
{
    private static readonly string[] PositiveWords =
    [
        // Inglés
        "surge", "surges", "surged", "soar", "soars", "soared", "jump", "jumps", "jumped",
        "rally", "rallies", "rallied", "gain", "gains", "gained", "rise", "rises", "risen", "rising",
        "beat", "beats", "beating", "outperform", "outperforms", "upgrade", "upgraded", "upgrades",
        "bullish", "boost", "boosts", "boosted", "record", "growth", "grows", "grew", "profit",
        "profits", "profitable", "strong", "strength", "recovery", "recovers", "recovered",
        "breakthrough", "approval", "approved", "approves", "win", "wins", "won", "winning",
        "success", "successful", "positive", "optimistic", "expansion", "expands", "expanded",
        "buy", "buys", "buying", "soars",
        // Español
        "sube", "subida", "gana", "ganancia", "ganancias", "impulsa", "impulsan", "alza",
        "positivo", "positiva", "crecimiento", "recuperacion", "recuperación", "record", "récord",
        "aprobado", "aprobada", "exito", "éxito", "exitoso"
    ];

    private static readonly string[] NegativeWords =
    [
        // Inglés
        "plunge", "plunges", "plunged", "plummet", "plummets", "plummeted", "fall", "falls",
        "fallen", "falling", "drop", "drops", "dropped", "slump", "slumps", "slumped", "loss",
        "losses", "miss", "misses", "missed", "downgrade", "downgraded", "downgrades", "bearish",
        "weak", "weakness", "decline", "declines", "declined", "sell", "sells", "selling", "sued",
        "lawsuit", "lawsuits", "fraud", "recall", "recalls", "recalled", "bankruptcy", "bankrupt",
        "layoff", "layoffs", "cut", "cuts", "cutting", "warning", "warns", "warned", "crash",
        "crashes", "crashed", "negative", "pessimistic", "investigation", "probe", "probes",
        "scandal", "delay", "delays", "delayed", "risk", "risks", "concern", "concerns",
        // Español
        "baja", "cae", "caida", "caída", "perdida", "pérdida", "perdidas", "pérdidas", "negativo",
        "negativa", "advertencia", "quiebra", "recorte", "recortes", "demanda", "investigacion",
        "investigación", "riesgo", "riesgos", "escandalo", "escándalo", "retraso"
    ];

    private static readonly Regex PositiveRegex = BuildRegex(PositiveWords);
    private static readonly Regex NegativeRegex = BuildRegex(NegativeWords);

    private static Regex BuildRegex(string[] words)
        => new(@"\b(?:" + string.Join('|', words.Distinct()) + @")\b",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public decimal Score(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0m;

        var positive = PositiveRegex.Matches(text).Count;
        var negative = NegativeRegex.Matches(text).Count;
        var total = positive + negative;
        if (total == 0) return 0m;

        return (decimal)(positive - negative) / total;
    }
}
