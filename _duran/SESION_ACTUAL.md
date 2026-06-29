# Sesión Actual

> **Propósito**: Mantener contexto entre sesiones de trabajo y usuarios.
> **Actualizar**: Al final de cada sesión con Claude.

---

## Estado de la Última Sesión

| Campo | Valor |
|-------|-------|
| **Fecha** | 2026-06-29 |
| **Usuario** | Francisco Javier Gonzalez Criado |
| **Evolutivo activo** | ninguno (HV-015..HV-019 cerrados) |
| **Duración aprox.** | larga |

---

## Control de versiones (2026-06-29)
- [x] Proyecto bajo **Git** e inicializado; commit base de todo (HV-001..HV-019) + `.gitattributes`; excluidos binarios SQLite.
- [x] Subido a **GitHub (repo privado)** `github.com/fjgonzalezcriado/EnsenameLaPasta` (rama `main`). Resuelto un cruce de credenciales (cuenta `RemitiraLaLluvia` cacheada por error) y merge del init remoto conservando nuestro `.gitignore`.

---

## Resumen de lo Trabajado (2026-06-29 · rentabilidad % + histórico + import + divisa)

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
Fase: 9 evolutivos completados (HV-001 … HV-009)
Tests: 89 verdes
Build: OK
```

### Tareas pendientes prioritarias
1. [ ] (Opcional) Fase 2: AlphaVantage / Binance (mismo patrón provider + histórico).
2. [ ] (Opcional) Fase 3: ML.NET para señales.
3. [ ] (Opcional) Pulidos: precisión de la cuenta atrás vía SignalR (push), importador de histórico pasado, retención por tiempo además de tamaño.

### Notas importantes
- **Go-dark activo**: no reinstalar context7 ni ejecutar comandos que contacten Comillas (ver memoria `go-dark-comillas`).
- El histórico de la barra de rangos requiere Internet (Yahoo `/v8/chart`); funciona con símbolos reales (AAPL, GOOG, BTCUSD→BTC-USD).
- Recargar la web con `Ctrl+F5` tras cambios de JS/CSS (.NET 10 sirve estáticos desde el build).

---

## Historial Reciente

| Fecha | Usuario | Trabajo principal |
|-------|---------|-------------------|
| 2026-06-16 | FJGC | HV-013 tracker de cartera real: alta/cierre/borrado de posiciones + modal de alta + buscador en modal · 103 tests |
| 2026-06-16 | FJGC | Pivote a tracker real: HV-010 buscador + HV-011 watchlist persistida + HV-012 feed 100% Yahoo (RandomWalk fuera, auto-trading off) · 96 tests |
| 2026-06-15/16 | FJGC | Go-dark Comillas + HV-008/009 + overhaul UI dashboard + retención BD + cuenta atrás |
| 2026-05-26 | FJGC | MVP completo (HV-001…006) + Fase 2 iniciada (HV-007) |

---

*Archivo de contexto de sesión - STIC.IA v3.9.0*
