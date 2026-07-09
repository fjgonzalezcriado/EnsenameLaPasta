# Mapa de Dependencias - Enseñame la Pasta

> Documenta dependencias entre componentes, paquetes NuGet y sistemas externos.
> Actualizar tras crear nuevas dependencias.

---

## Diagrama de Arquitectura (Clean Architecture)

```
+-------------------------------------------------------+
|  Web (ASP.NET Core MVC)                               |
|    Controllers -> Views (Razor) + ViewModels          |
+----------------------------+--------------------------+
                             |
                             v
+-------------------------------------------------------+
|  Application                                          |
|    Services, Strategies, Interfaces, DTOs             |
|    (NO depende de Infrastructure)                     |
+----------------------------+--------------------------+
                             |
                             v
+-------------------------------------------------------+
|  Domain                                               |
|    Entities (Trade, MarketTick, PortfolioSnapshot)    |
|    Enums (TradeStatus)                                |
|    Value Objects, reglas de negocio                   |
|    (Sin dependencias externas)                        |
+-------------------------------------------------------+
                             ^
                             |
+----------------------------+--------------------------+
|  Infrastructure                                       |
|    EF Core DbContext, Migrations, Repositorios        |
|    BackgroundServices (MarketTickGenerator, etc.)     |
|    Implementa interfaces de Application               |
+--------------------+----------------------------------+
                     |
                     v
              +-------------+
              |   SQLite    |
              | trading.db  |
              +-------------+
```

---

## Matriz de Impacto

| Si modificas... | Revisa impacto en... | Riesgo | Tests a ejecutar |
|---|---|---|---|
| Domain/Entities | Application, Infrastructure (EF mappings), Migrations | Alto | Unit + Migration check |
| Domain/Enums (TradeStatus) | Repositorios, Views, ViewModels | Alto | Unit + UI |
| Application/Services | Web/Controllers, BackgroundServices, Tests | Medio | Unit |
| Application/Strategies | StrategyExecutionService | Medio | Unit estrategias |
| Infrastructure/Repositories | Services, Migrations | Alto | Integration |
| Infrastructure/BackgroundServices | Pipeline runtime (DI registration en Program.cs) | Medio | Manual / Integration |
| Web/Controllers | Views, ViewModels, rutas | Bajo | UI / E2E manual |
| EF Migrations | Estado de trading.db; obliga update-database | Critico | Migration + smoke test |
| appsettings.json (capital, frecuencia, simbolos, MA windows) | Comportamiento del simulador en runtime | Medio | Smoke test |

---

## Dependencias por Modulo

### Modulo: Dashboard

**Depende de**:
| Componente | Tipo | Criticidad | Notas |
|---|---|---|---|
| PortfolioService | Application service | Alta | Lectura de capital actual, pnl, metricas |
| TradeRepository | Infrastructure | Alta | Listado de operaciones abiertas/cerradas |
| PortfolioSnapshot (entidad) | Domain | Media | Para grafico de evolucion |
| Chart.js | Frontend (CDN) | Baja | Renderizado de grafico |

### Modulo: Simulacion de Mercado

**Depende de**:
| Componente | Tipo | Criticidad | Notas |
|---|---|---|---|
| MarketTick (entidad) | Domain | Alta | Modelo a generar |
| IMarketDataProvider | Application interface | Alta | Permite swap futuro a APIs reales |
| BackgroundService (host) | .NET hosting | Alta | Lifecycle del generador |
| appsettings.json | Configuration | Media | Frecuencia, simbolos, parametros aleatorios |

### Modulo: Estrategia MA Crossover

**Depende de**:
| Componente | Tipo | Criticidad | Notas |
|---|---|---|---|
| IMarketDataProvider | Application | Alta | Fuente de ticks |
| OrderService (Buy/Sell) | Application | Alta | Ejecucion de senales |
| Historial de ticks en memoria/BD | Performance | Media | Para calcular MAs |

### Modulo: Portfolio

**Depende de**:
| Componente | Tipo | Criticidad | Notas |
|---|---|---|---|
| TradeRepository | Infrastructure | Alta | Calculo pnl realizado |
| MarketTick actual | Application | Alta | Valoracion de posiciones abiertas |
| Capital inicial | Configuration | Media | appsettings.json |

---

## Integraciones Externas

> **Estado actual**: NO hay integraciones externas en el MVP. Lista de roadmap.

### [ROADMAP - Fase 2] Yahoo Finance
- Tipo: API REST publica
- Direccion: Consumimos
- Endpoint base: https://query1.finance.yahoo.com/
- Auth: Ninguna / Token gratuito
- Limites: Rate limit no documentado
- Reemplaza: MarketTickGeneratorService fake

### [ROADMAP - Fase 2] Binance
- Tipo: API REST + WebSocket
- Direccion: Consumimos
- Endpoint base: https://api.binance.com/
- Auth: API Key (registro gratuito)
- Tipo de datos: Cripto en tiempo real

### [ROADMAP - Fase 2] AlphaVantage
- Tipo: API REST
- Direccion: Consumimos
- Endpoint base: https://www.alphavantage.co/query
- Auth: API Key (5 req/min gratuito)
- Uso: Datos historicos para backtesting riguroso

---

## Paquetes NuGet (planificados al hacer scaffold)

### Web (EnsenameLaPasta.Web)
| Paquete | Version (target) | Proposito |
|---|---|---|
| Microsoft.AspNetCore.App | 10.0.* | Framework MVC |
| Microsoft.EntityFrameworkCore.Sqlite | 10.0.* | Provider SQLite |
| Microsoft.EntityFrameworkCore.Design | 10.0.* | dotnet ef migrations |

### Application
| Paquete | Version (target) | Proposito |
|---|---|---|
| Microsoft.Extensions.Hosting.Abstractions | 10.0.* | BackgroundService, IHostedService |
| Microsoft.Extensions.Options | 10.0.* | Options Pattern |
| Microsoft.Extensions.Logging.Abstractions | 10.0.* | ILogger<T> |

### Infrastructure
| Paquete | Version (target) | Proposito |
|---|---|---|
| Microsoft.EntityFrameworkCore | 10.0.* | ORM |
| Microsoft.EntityFrameworkCore.Sqlite | 10.0.* | Provider SQLite |

### Domain
Sin paquetes (capa pura, solo BCL).

### Tests (EnsenameLaPasta.Tests)
| Paquete | Version (target) | Proposito |
|---|---|---|
| xunit | 2.* | Framework de tests |
| FluentAssertions | 6.* | Asserts legibles |
| Moq | 4.* | Mocks |
| Microsoft.EntityFrameworkCore.InMemory | 10.0.* | DbContext en memoria para tests |

### Frontend (sin NuGet, via CDN o LibMan)
| Recurso | Version | Proposito |
|---|---|---|
| Bootstrap | 5.x | Layout y componentes UI |
| Chart.js | 4.x | Graficos de evolucion |
| jQuery | 3.x (opcional) | Solo si se necesita interactividad clasica |

---

## Reglas y restricciones de dependencias

1. **Domain** no depende de nadie (ni siquiera EF Core).
2. **Application** depende solo de Domain y de abstracciones del runtime (Microsoft.Extensions.*).
3. **Infrastructure** implementa interfaces de Application, depende de Domain.
4. **Web** depende de Application (y Infrastructure solo para registro de DI).
5. Sin librerias de logging de terceros (Serilog/NLog) — usar Microsoft.Extensions.Logging.
6. Sin Docker, sin microservicios, sin Kubernetes (regla de proyecto).

---

**Ultima actualizacion**: 2026-05-26
**Actualizado por**: stic.claude3 (via /onboarding)
