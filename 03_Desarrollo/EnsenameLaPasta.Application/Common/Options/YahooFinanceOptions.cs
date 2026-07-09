namespace EnsenameLaPasta.Application.Common.Options;

public sealed class YahooFinanceOptions
{
    public const string SectionName = "MarketData:YahooFinance";

    /// <summary>URL base del API público de Yahoo Finance.</summary>
    public string BaseUrl { get; set; } = "https://query1.finance.yahoo.com";

    /// <summary>Timeout HTTP en segundos.</summary>
    public int TimeoutSeconds { get; set; } = 10;
}
