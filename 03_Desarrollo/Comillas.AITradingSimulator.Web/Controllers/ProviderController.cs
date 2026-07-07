using Comillas.AITradingSimulator.Application.Common.Interfaces;
using Comillas.AITradingSimulator.Application.Common.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Comillas.AITradingSimulator.Web.Controllers;

/// <summary>
/// Consulta y cambio (en runtime) del proveedor de datos de mercado activo (HV-033).
/// </summary>
public sealed class ProviderController : Controller
{
    private readonly IMarketProviderState _state;
    private readonly IOptionsMonitor<TwelveDataOptions> _twelveData;
    private readonly IOptionsMonitor<AlphaVantageOptions> _alphaVantage;

    public ProviderController(
        IMarketProviderState state,
        IOptionsMonitor<TwelveDataOptions> twelveData,
        IOptionsMonitor<AlphaVantageOptions> alphaVantage)
    {
        _state = state;
        _twelveData = twelveData;
        _alphaVantage = alphaVantage;
    }

    [HttpGet("/api/provider")]
    public IActionResult Get() => Json(new
    {
        current = _state.Current,
        available = _state.Available,
        twelveDataKeyConfigured = !string.IsNullOrWhiteSpace(_twelveData.CurrentValue.ApiKey),
        alphaVantageKeyConfigured = !string.IsNullOrWhiteSpace(_alphaVantage.CurrentValue.ApiKey)
    });

    [HttpPost("/api/provider")]
    public IActionResult Set([FromBody] ProviderRequest? request)
    {
        var provider = request?.Provider?.Trim();
        if (string.IsNullOrWhiteSpace(provider)
            || !_state.Available.Any(a => string.Equals(a, provider, StringComparison.OrdinalIgnoreCase)))
        {
            return BadRequest($"Proveedor no válido. Disponibles: {string.Join(", ", _state.Available)}.");
        }

        _state.Set(provider);
        return Json(new { current = _state.Current });
    }

    public sealed record ProviderRequest(string? Provider);
}
