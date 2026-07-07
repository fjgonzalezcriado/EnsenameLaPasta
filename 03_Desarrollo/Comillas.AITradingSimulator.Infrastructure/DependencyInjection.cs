using Comillas.AITradingSimulator.Application.Common.Interfaces;
using Comillas.AITradingSimulator.Application.Common.Options;
using Comillas.AITradingSimulator.Application.Services;
using Comillas.AITradingSimulator.Application.Strategies;
using Comillas.AITradingSimulator.Infrastructure.MarketData;
using Comillas.AITradingSimulator.Infrastructure.Persistence;
using Comillas.AITradingSimulator.Infrastructure.Strategy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Comillas.AITradingSimulator.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("ConnectionString 'Default' no configurada en appsettings.json.");

        // Persistence
        services.AddDbContext<TradingDbContext>(options =>
            options.UseSqlite(connectionString));
        services.AddScoped<ITradingDbContext>(sp => sp.GetRequiredService<TradingDbContext>());
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IDashboardService, DashboardService>();

        // Market data simulation / fetching
        services.AddOptions<MarketDataOptions>()
            .Bind(configuration.GetSection(MarketDataOptions.SectionName))
            .ValidateOnStart();

        // Estado del proveedor activo, conmutable en runtime desde la UI (HV-033).
        // Valor inicial: MarketData:ProviderType (o el persistido en App_Data/active-provider.txt).
        services.AddSingleton<IMarketProviderState, MarketProviderState>();

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ITickBus, ChannelTickBus>();

        // Cliente Yahoo + histórico: disponibles SIEMPRE (incluso con RandomWalk
        // en vivo), porque la barra de rangos del dashboard consume el histórico real.
        services.AddOptions<YahooFinanceOptions>()
            .Bind(configuration.GetSection(YahooFinanceOptions.SectionName))
            .ValidateOnStart();
        services.AddHttpClient(YahooFinanceProvider.HttpClientName, (sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<YahooFinanceOptions>>().Value;
            client.BaseAddress = new Uri(opts.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
            // UA de navegador: /v8/chart responde mejor con UA realista.
            client.DefaultRequestHeaders.TryAddWithoutValidation(
                "User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0 Safari/537.36");
        });
        // Histórico (barra de rangos): ambos proveedores registrados; el activo lo resuelve
        // en runtime SelectableMarketHistoryProvider (coherente con el feed en vivo).
        services.AddSingleton<YahooHistoryProvider>();
        services.AddSingleton<TwelveDataHistoryProvider>();
        services.AddSingleton<AlphaVantageHistoryProvider>();
        services.AddSingleton<IMarketHistoryProvider, SelectableMarketHistoryProvider>();

        // Búsqueda de instrumentos: ambos proveedores + selector por proveedor activo,
        // para que los resultados usen la convención de símbolos del feed en uso (HV-034).
        services.AddSingleton<YahooInstrumentSearchProvider>();
        services.AddSingleton<TwelveDataInstrumentSearchProvider>();
        services.AddSingleton<AlphaVantageInstrumentSearchProvider>();
        services.AddSingleton<IInstrumentSearchProvider, SelectableInstrumentSearchProvider>();

        // Conversión de divisas (totales en divisa base). Usa el HttpClient de Yahoo.
        services.AddOptions<FxOptions>()
            .Bind(configuration.GetSection(FxOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IFxRateProvider, FxRateProvider>();
        // Refresco de tipos en background (saca la llamada HTTP del hot path del dashboard).
        services.AddHostedService<FxRefreshService>();

        // Watchlist (instrumentos seguidos). El generador lee de aquí en cada ciclo.
        services.AddScoped<IWatchlistService, WatchlistService>();

        // Posiciones manuales del usuario (tracker de cartera real).
        services.AddScoped<IPositionService, PositionService>();

        // Caja / efectivo (ingresos y retiradas).
        services.AddScoped<ICashService, CashService>();

        // Segundo proveedor: Twelve Data (requiere API key). HttpClient siempre registrado;
        // solo se usa si es el proveedor activo.
        services.AddOptions<TwelveDataOptions>()
            .Bind(configuration.GetSection(TwelveDataOptions.SectionName))
            .ValidateOnStart();
        services.AddHttpClient(TwelveDataProvider.HttpClientName, (sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<TwelveDataOptions>>().Value;
            client.BaseAddress = new Uri(opts.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
        });

        // Tercer proveedor: Alpha Vantage (requiere API key; plan gratuito muy limitado,
        // 25 req/día). HttpClient siempre registrado; solo se usa si es el proveedor activo.
        services.AddOptions<AlphaVantageOptions>()
            .Bind(configuration.GetSection(AlphaVantageOptions.SectionName))
            .ValidateOnStart();
        services.AddHttpClient(AlphaVantageProvider.HttpClientName, (sp, client) =>
        {
            var opts = sp.GetRequiredService<IOptions<AlphaVantageOptions>>().Value;
            client.BaseAddress = new Uri(opts.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(opts.TimeoutSeconds);
        });

        // Feed EN VIVO: los proveedores registrados; el activo lo resuelve en runtime
        // SelectableMarketDataProvider. La simulación RandomWalk se retiró.
        services.AddSingleton<YahooFinanceProvider>();
        services.AddSingleton<TwelveDataProvider>();
        services.AddSingleton<AlphaVantageProvider>();
        services.AddSingleton<IMarketDataProvider, SelectableMarketDataProvider>();
        services.AddHostedService<MarketTickGeneratorService>();

        // Estrategia automática MA Crossover DESACTIVADA: el panel es ahora un visor
        // de precios reales (sin auto-trading). El código de la estrategia se conserva
        // por si se reactiva; basta volver a registrar StrategyExecutionService.

        // Retención de la BD por tamaño (purga + VACUUM)
        services.AddOptions<RetentionOptions>()
            .Bind(configuration.GetSection(RetentionOptions.SectionName))
            .ValidateOnStart();
        services.AddHostedService<DatabaseRetentionService>();

        // Snapshots periódicos del valor de cuenta (histórico de la cuenta)
        services.AddOptions<SnapshotOptions>()
            .Bind(configuration.GetSection(SnapshotOptions.SectionName))
            .ValidateOnStart();
        services.AddHostedService<PortfolioSnapshotService>();

        return services;
    }
}
