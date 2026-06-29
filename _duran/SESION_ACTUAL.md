# Sesión Actual

> **Propósito**: Mantener contexto entre sesiones de trabajo y usuarios.
> **Actualizar**: Al final de cada sesión con Claude.

---

## Estado de la Última Sesión

| Campo | Valor |
|-------|-------|
| **Fecha** | 2026-06-29 |
| **Usuario** | Francisco Javier Gonzalez Criado |
| **Evolutivo activo** | ninguno (HV-015..HV-023 cerrados) |
| **Duración aprox.** | muy larga (9 evolutivos + alta en GitHub) |

---

## Control de versiones (2026-06-29)
- [x] Proyecto bajo **Git** e inicializado; commit base de todo (HV-001..HV-019) + `.gitattributes`; excluidos binarios SQLite.
- [x] Subido a **GitHub (repo privado)** `github.com/fjgonzalezcriado/EnsenameLaPasta` (rama `main`). Resuelto un cruce de credenciales (cuenta `RemitiraLaLluvia` cacheada por error) y merge del init remoto conservando nuestro `.gitignore`.

---

## Resumen de lo Trabajado (2026-06-29 · rentabilidad % + histórico + import + divisa + FX + caja FX + PnL base)

### HV-023 · Refrescar FX en background
- [x] `IFxRateProvider.RefreshAsync` (fuerza fetch ignorando TTL) + `FxRefreshService : BackgroundService` que refresca las divisas en uso (watchlist + caja) cada `Fx:RefreshSeconds` (300).
- [x] El dashboard ya no llama a la fuente FX en el hot path: lee de caché siempre caliente.
- [x] 3 tests nuevos; **135 verdes**. Smoke real: arranque sin errores. Rama `feature/HV-023-fx-background`.

### HV-022 · PnL convertido por fila
- [x] `OpenTradeDto.UnrealizedPnLBase`/`ClosedTradeDto.RealizedPnLBase` (PnL × tipo del símbolo → base); se reordenó el bloque FX (`RateOf`) para construirlo antes de las filas.
- [x] UI: helper `pnlCell` muestra el equivalente en base (`≈ …`) solo si la divisa de la fila ≠ base.
- [x] 1 test nuevo; **132 verdes**. Sin migración. Rama `feature/HV-022-pnl-convertido`.

### HV-021 · Divisa por movimiento de caja
- [x] `CashMovement.Currency` (default EUR) + migración `AddCashMovementCurrency` (defaultValue "EUR" para filas previas).
- [x] `CashMovementDto`/`ICashService.AddAsync`/`CashController` aceptan divisa; `DashboardService` convierte `netDeposits` por divisa de cada movimiento (invariante preservado).
- [x] UI: campo **Divisa** en el alta del modal Caja, importe por fila en su divisa y "Aportado neto" agrupado por divisa.
- [x] 2 tests nuevos; **131 verdes**. Smoke real: alta 100 USD → netDeposits 30000→30087,67 (×0,8767), divisa USD, DELETE 204, vuelve a 30000.
- [x] Rama `feature/HV-021-divisa-caja`. Completa la deuda de HV-020.

### HV-020 · Conversión FX a divisa base
- [x] `IFxRateProvider`/`FxRateProvider` (Yahoo `{FROM}{TO}=X`, caché por par con TTL, degradación a 1).
- [x] `DashboardService` convierte los totales a `Fx:BaseCurrency` (EUR) por el tipo de cada símbolo; invariante `accountValue = netDeposits + totalPnL` preservado. `DashboardDto.BaseCurrency` + nota en UI.
- [x] Cierra la deuda de divisas de HV-019 (los totales ya no mezclan monedas). Aportaciones de caja asumidas en base.
- [x] 6 tests nuevos; **129 verdes**. Smoke real: `USDEUR=X=0,8768`, dashboard `base=EUR`.
- [x] Desarrollado en rama `feature/HV-020-conversion-fx`.

### HV-019 · Divisa por instrumento
- [x] `TrackedSymbol.Currency` + `SetCurrency` + migración `AddTrackedSymbolCurrency`.
- [x] `IMarketDataProvider.GetLatestAsync` → `MarketQuote(Tick, Currency)` (Yahoo `meta.currency`); el `MarketTickGeneratorService` **sella** la divisa de cada símbolo seguido cada ciclo (~30 s).
- [x] `Currency` en `TrackedSymbolDto`/`OpenTradeDto`/`ClosedTradeDto`. UI: columna "Div" + formateo monetario por fila en su divisa.
- [x] **Limitación**: los totales de cuenta siguen en € (mezclan divisas hasta el paso FX).
- [x] 3 tests nuevos; **123 verdes**. Smoke real: `HY9H.F→EUR`, `^GSPC→USD`.

### HV-018 · Importar histórico de compras (CSV)
- [x] `IPositionService.ImportCsvAsync` (reutiliza `OpenAsync`): parser con cabecera opcional, delimitador autodetectado `;`/`,` (con `;` admite coma decimal), fechas múltiples (UTC); filas con error se reportan por línea sin abortar el resto.
- [x] `ImportResultDto`/`ImportErrorDto` + endpoint `POST /api/positions/import`. UI: modal "📥 Importar CSV" (subir fichero o pegar).
- [x] 4 tests nuevos; **120 verdes**. Smoke real: endpoint reporta error por línea (fila inválida, sin escribir en BD).

### HV-017 · Histórico del valor de cuenta
- [x] `PortfolioSnapshotService : BackgroundService` toma un snapshot al arrancar (15 s) y cada `IntervalSeconds` (300, sección `Snapshot`), reutilizando `IDashboardService`. **Sin migración** (entidad `PortfolioSnapshot` ya existía).
- [x] `AccountHistoryPointDto` + `IDashboardService.GetAccountHistoryAsync` (deriva `NetDeposits`/`ReturnPct`) + endpoint `GET /api/account/history?points=`.
- [x] UI: tarjeta "📊 Evolución del valor de cuenta" con gráfico Chart.js (valor de cuenta + aportado neto), refresco propio 60 s.
- [x] 5 tests nuevos; **116 verdes**. Smoke real OK: `/api/account/history` → punto coherente (28.306,65 € / aportado 30.000 / −5,64 %).

### HV-016 · Rentabilidad por posición
- [x] `OpenTradeDto.ReturnPct` y `ClosedTradeDto.ReturnPct` calculados en `DashboardService` (`(precio−entry)/entry×100`, 0 % si entry=0).
- [x] UI: nueva columna **"%"** tras PnL en las tablas de posiciones abiertas y trades cerrados, con signo y color (reusa `pctSigned`/`signClass`).
- [x] 2 tests nuevos; **113 verdes**. Sin migración ni cambios de dominio/BD.
- [ ] Pendiente: smoke real manual (HY9H.F 1360→1390 → +2,21 %).

### HV-015 · Rentabilidad porcentual de la cuenta
- [x] `DashboardDto.ReturnPct` + cálculo en `DashboardService` (`TotalPnL / NetDeposits × 100`, 0 % si no hay aportaciones).
- [x] UI: el subtítulo de la tarjeta **Valor de cuenta** muestra el **% con signo y color** + "sobre aportado" (reutiliza `pctSigned`/`setSigned`).
- [x] 3 tests nuevos; **111 verdes**. Sin migración ni cambios de dominio/BD.
- [ ] Pendiente: smoke real manual con la app en marcha (aporta + posición → % coherente).

---

## Resumen de lo Trabajado (2026-06-16 · pivote a tracker real)

### HV-013 · Tracker de cartera real (lo último)
- [x] **Alta manual de posiciones reales** (modal "➕ Nueva posición": símbolo con datalist de la watchlist, precio de entrada, cantidad, fecha) para dejar de usar TradingView.
- [x] **Cerrar** (precio de cierre, default = precio actual) y **eliminar** posiciones desde la tabla (columna Acciones).
- [x] `IPositionService`/`PositionService` (por Id; varias posiciones por símbolo; auto-añade a watchlist) + endpoints REST de posiciones. **PnL en vivo** vs precio real.
- [x] **Buscador** convertido en **modal** (botón "🔎 Buscar / añadir instrumento"; se cierra al añadir).
- [x] 103 tests verdes; smoke real HY9H.F 1360×5 → +150 € (actual 1390), cierre 1400 → +200 €.

### Objetivos cumplidos (sesión actual)
- [x] **Seguir HY9H.F** (SK hynix Inc., Frankfurt; entrada 1360 €): añadido como símbolo y, tras el pivote, como semilla de la watchlist.
- [x] **HV-010 Buscador de instrumentos**: por descripción / ISIN / ticker vía Yahoo `/v1/finance/search` (WKN no soportado por Yahoo → fuera de alcance). Endpoint + UI.
- [x] **HV-011 Watchlist persistida**: entidad `TrackedSymbol` + migración + `WatchlistService`; el generador lee símbolos de BD; alta/baja desde el buscador; seed `HY9H.F`.
- [x] **HV-012 Feed 100% real**: retirada de la simulación RandomWalk; proveedor Yahoo siempre; estrategia MA **desactivada** (panel solo-visor); intervalo 30 s.
- [x] **96 tests verdes** + smoke real (HY9H.F = 1390 €).
- [x] Reconstruido `ESTADO_PROYECTO.json` (apareció a 0 bytes por causa externa) desde el import de CLAUDE.md.

### Decisiones tomadas (sesión actual)
- **WKN fuera de alcance** (Yahoo no lo indexa). Búsqueda por descripción/ISIN/ticker.
- **Eliminar simulación**: solo datos reales (requiere Internet).
- **Panel solo-visor**: sin auto-trading (código de estrategia conservado, solo se desregistra el hosted service).

---

## Resumen anterior (2026-06-15/16)

### Objetivos cumplidos
- [x] **GO-DARK de Comillas**: baja RGPD del hub STIC.IA (`/v2/forget`), borrado de credenciales y maquinaria MCP/telemetría/sync, neutralización de llamadas de red en hooks. El proyecto ya no contacta `*.comillas.edu`.
- [x] **HV-008**: fix `YahooFinanceProvider` (migrado de `/v7/quote` —401— a `/v8/chart`).
- [x] **Overhaul UI dashboard**: ancho completo, eliminación de Home/Privacy, modo oscuro, formato es-ES (€), estado del feed (proveedor/ticks/BD/último), selectores de refresco/símbolo/histórico (desacoplados), rejilla adaptativa, eje de precio a la derecha con etiqueta de valor, barra de rangos.
- [x] **Retención de BD por tamaño** (1 GB por defecto, purga de ticks antiguos + VACUUM).
- [x] **HV-009**: histórico real de Yahoo en la barra de rangos (1D…5A) + endpoint `/api/history`.
- [x] **Cuenta atrás** al próximo refresco del gráfico (relativa al selector "Gráfico cada").

### Archivos modificados (principales)
- `03_Desarrollo/.../Web/wwwroot/js/dashboard.js` - lógica del dashboard (gráfico, selectores, histórico, cuenta atrás)
- `03_Desarrollo/.../Web/Views/{Shared/_Layout,Dashboard/Index}.cshtml` - layout + dashboard
- `03_Desarrollo/.../Web/wwwroot/css/site.css` - paleta Comillas, tema, overlay
- `03_Desarrollo/.../Application/...` - `IMarketHistoryProvider`, `DashboardDto`, `DashboardService`, opciones (`MarketData`, `Retention`)
- `03_Desarrollo/.../Infrastructure/...` - `YahooFinanceProvider`, `YahooHistoryProvider`, `DatabaseRetentionService`, DI, `TradingDbContext`
- `_duran/` - specs HV-008/009, FUNCIONALIDADES, HISTORIAL_CAMBIOS, LECCIONES, ESTADO_PROYECTO

### Decisiones tomadas
- **D1**: Proyecto desconectado de Comillas (go-dark) — es personal, fuera de scope. NO ejecutar arranque/sync/register/actualizar.
- **D2**: Gráfico en modo rango muestra precios reales de Yahoo; métricas/trades siguen siendo del simulador (viewer de mercado sobre la simulación).
- **D3**: Retención por tamaño de fichero (no por tiempo), límite configurable.

---

## Contexto para Próxima Sesión

### Estado del evolutivo actual
```
ID: ninguno
Fase: 23 evolutivos completados (HV-001 … HV-023)
Tests: 135 verdes
Build: OK · versión 1.12.0-fx-background
Git: main = origin/main (50926cd), GitHub privado, árbol limpio
```

### Tareas pendientes prioritarias
1. [ ] (Opcional) Importar **trades cerrados** por CSV (con exit/fecha de cierre).
2. [ ] (Opcional) **Dashboard visor puro** / pulidos de UI.
3. [ ] (Opcional) Fase 3: AlphaVantage / Binance (2º proveedor, mismo patrón) y ML.NET para señales.
4. [ ] (Opcional) Métrica/health del último refresco FX; backoff si la fuente FX falla repetidamente.
5. [ ] Smoke real manual pendiente (no bloqueante) de HV-015/HV-016 con la app en marcha.

### Notas importantes
- **Go-dark activo**: no reinstalar context7 ni ejecutar comandos que contacten Comillas (ver memoria `go-dark-comillas`). Yahoo Finance **sí** está permitido (feed y FX).
- **Tras un smoke con `dotnet run`, matar el proceso `Comillas.AITradingSimulator.Web` antes de recompilar** o el build falla por DLL bloqueada (MSB3027). Ver memoria `smoke-test-mata-proceso-web`.
- Editar valores con `>` dentro de `ESTADO_PROYECTO.json` falla con Edit (PowerShell los escribe como `>`); usar PowerShell regex y validar con `ConvertFrom-Json`.
- FX y feed requieren Internet (Yahoo). Recargar la web con `Ctrl+F5` tras cambios de JS/CSS.
- Credenciales GitHub cacheadas correctamente (cuenta `fjgonzalezcriado`); puedo commitear/push directamente.

---

## Historial Reciente

| Fecha | Usuario | Trabajo principal |
|-------|---------|-------------------|
| 2026-06-29 | FJGC | HV-015..HV-023 (rentabilidad %, por posición, histórico de cuenta, importar CSV, divisas: instrumento→FX→caja→por fila→background) + alta del repo en GitHub privado · 135 tests |
| 2026-06-16 | FJGC | HV-013 tracker de cartera real: alta/cierre/borrado de posiciones + modal de alta + buscador en modal · 103 tests |
| 2026-06-16 | FJGC | Pivote a tracker real: HV-010 buscador + HV-011 watchlist persistida + HV-012 feed 100% Yahoo (RandomWalk fuera, auto-trading off) · 96 tests |
| 2026-06-15/16 | FJGC | Go-dark Comillas + HV-008/009 + overhaul UI dashboard + retención BD + cuenta atrás |
| 2026-05-26 | FJGC | MVP completo (HV-001…006) + Fase 2 iniciada (HV-007) |

---

*Archivo de contexto de sesión - STIC.IA v3.9.0*
