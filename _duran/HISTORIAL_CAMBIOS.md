# Historial de Cambios (Changelog)

> **INSTRUCCIONES PARA CLAUDE**: Este archivo se actualiza automaticamente con /commit y /prepara-entrega.
> Documenta todos los cambios significativos del proyecto.

---

## Formato de Entradas

Seguimos el formato [Keep a Changelog](https://keepachangelog.com/es-ES/1.0.0/):

- **Added**: Nuevas funcionalidades
- **Changed**: Cambios en funcionalidades existentes
- **Deprecated**: Funcionalidades que seran eliminadas
- **Removed**: Funcionalidades eliminadas
- **Fixed**: Correcciones de bugs
- **Security**: Correcciones de seguridad

---

## [1.35.0-metricas-avanzadas] - 2026-07-07

### Added
- 📐 **HV-048** Métricas avanzadas de cartera: **Max Drawdown** (+ actual), **Sharpe** y **volatilidad** anualizados y **Profit Factor**. Endpoint `GET /api/account/metrics` + tarjeta en el panel. Cálculo sobre el índice de retorno (neutral a aportaciones); Sharpe/vol con gate ≥ 10 retornos diarios (si no, "insuficiente").

### Métricas
- 185 tests verdes (181 + 4). Smoke real: Max Drawdown −7,69 % mostrado; Sharpe/vol insuficientes con 3 días (honesto). Corrección: el cálculo inicial sobre valor de cuenta bruto daba cifras absurdas por las aportaciones → índice de retorno.

---

## [1.34.0-import-cerrados] - 2026-07-07

### Added
- 📥 **HV-047** Importar **trades cerrados** por CSV: `symbol, entry, qty[, date[, exit[, closeDate]]]`. Si la fila trae `exit`, se importa como trade cerrado (abre+cierra); si no, posición abierta (compat HV-018). Validación por fila sin dejar posiciones a medio abrir.

### Métricas
- 181 tests verdes (178 + 3). Smoke: endpoint valida el formato extendido sin escribir en la cartera (cierre<apertura y exit inválido reportan error). Camino feliz cubierto por tests unitarios.

---

## [1.33.0-visor-puro] - 2026-07-07

### Added
- 👁 **HV-046** Modo **visor puro** (botón "👁 Visor"): pone el panel en solo lectura ocultando los controles de edición (proveedor, buscar/añadir, caja, importar CSV, nueva posición, quitar símbolo, columna Acciones). Conserva los controles de vista y la visualización. Persistido entre sesiones.

### Métricas
- 178 tests verdes (sin cambios; solo frontend). Smoke Playwright: OFF muestra todo, ON oculta edición, persiste tras reload, 0 errores de consola.

---

## [1.32.0-ml-features-modelos] - 2026-07-07

### Added
- 🧠 **HV-045** Clasificador ampliado: **12 features** (añade momentum 10p, precio vs SMA20, MACD, Bollinger %B, estocástico %K) y **2 modelos** (SDCA + **FastTree**) con **selección por AUC** en hold-out. `DirectionSignal.ModelUsed` + tooltip con modelo/features/accuracy/AUC.

### Métricas
- 178 tests verdes. Smoke real: `HY9H.F 6M → SDCA` (AUC 0,65); `MBOT 1A → FastTree` (acc 0,62) — distintos símbolos eligen distinto modelo. Calidad 0,50–0,65 (nivel azar-ish), reportada con transparencia.

---

## [1.31.0-ml-clasificacion] - 2026-07-07

### Added
- 📊 **HV-044** Clasificación **sube/baja** con ML.NET (SDCA + 7 features técnicas: retorno, momentum, medias, RSI, volatilidad, volumen). Split cronológico 80/20 con accuracy/AUC en hold-out (honestidad de calidad). `GET /api/signal` + badge 📊 (Comprar/Vender/Mantener + probabilidad) bajo el switch "🔮 Señales ML", con la calidad en el tooltip.

### Métricas
- 178 tests verdes (175 + 3). Smoke real (Yahoo): `HY9H.F 6M` → Comprar (Sube 68%), accuracy 0,61 / AUC 0,64 (n=88); `1D` → insuficiente. Nota: indicador educativo, no asesoramiento (la dirección de precio ronda el azar).

---

## [1.30.0-mlnet-senales] - 2026-07-07

### Added
- 🔮 **HV-043** Señales con **ML.NET** (`Microsoft.ML.TimeSeries`, SSA): pronóstico del cierre a N puntos con banda de confianza y señal Alcista/Bajista/Neutral. `GET /api/forecast` + switch "🔮 Pronóstico" con overlay (línea + banda) en el gráfico.

### Changed
- 🔧 `Directory.Build.props` nuevo: suprime el advisory NuGet `GHSA-2m69-gcr7-jv3q` (vuln. transitiva de la lib nativa de SQLite vía EF Core; preexistente, destapada por el restore de ML.NET). Auditoría activa para el resto.

### Métricas
- 175 tests verdes (171 + 4 nuevos de SSA). Smoke real (Yahoo): `HY9H.F 6M` → Alcista +39 % con banda; degradación limpia (histórico insuficiente / símbolo inválido). Caveat: SSA en series cortas extrapola con fuerza (banda ancha refleja incertidumbre).

---

## [1.29.0-alphavantage] - 2026-07-07

### Added
- 🟢 **HV-042** Tercer proveedor de datos: **Alpha Vantage** (feed `GLOBAL_QUOTE`, histórico `TIME_SERIES_*` con OHLC, búsqueda `SYMBOL_SEARCH`), seleccionable en runtime como Yahoo/Twelve Data. Opción en el selector + aviso "⚠ sin API key". Mensaje claro en límite de cuota; histórico/búsqueda degradan a vacío sin key.

### Métricas
- 171 tests verdes (156 + 15 nuevos; `MarketProviderStateTests` adaptado a 3 proveedores). Smoke de cableado: DI arranca con 3 proveedores, switch refleja en dashboard, degradación limpia sin key, POST inválido→400, UI con aviso.
- **Smoke real (API key)**: histórico IBM 1M = 22 velas OHLC reales; búsqueda "microsoft" = 6 resultados reales; feed degrada limpio. Limitación confirmada: AV gratuito = 25 req/día + 1 req/segundo (el feed en vivo tripea la ráfaga → usar Yahoo para el feed continuo).

---

## [1.28.0-velas-japonesas] - 2026-07-06

### Added
- 🕯️ **HV-041** Switch **línea ↔ velas japonesas** en el gráfico de precios. Velas OHLC (verde/rojo, mecha + cuerpo) dibujadas con un plugin propio sobre el eje de categorías (sin huecos de findes). Tooltip Apert./Máx/Mín/Cierre; persistido entre sesiones.

### Changed
- 🔧 Backend: `PricePoint` gana `Open/High/Low`; `YahooHistoryProvider` y `TwelveDataHistoryProvider` parsean OHLC (fallback al cierre).

### Métricas
- 156 tests verdes (154 + 2 de parseo OHLC). Smoke Playwright: velas ON → 21 velas en 1M, techo/suelo 1725/1060, línea oculta; OFF → línea restaurada; persistencia OK; 0 errores.

---

## [1.27.1-margen-selector-derecha] - 2026-07-06

### Changed
- 💅 Ajuste UI: el selector **Margen Y** se alinea al extremo derecho de la fila de rangos (`ms-auto`); la barra de rangos y el hint quedan a la izquierda. Solo `.cshtml`, sin lógica.

---

## [1.27.0-margen-eje-y] - 2026-07-06

### Changed
- 🎛️ **HV-040** El margen del eje Y del gráfico de precios pasa de fijo (±20) a **seleccionable** (`Margen Y`: Ajustado/±1/±2/±5/±10/±20 %) y **relativo al precio** (compresión ↔ extensión). Se recalcula al instante sin recargar el histórico y se conserva entre sesiones.

### Métricas
- 154 tests verdes (solo frontend). Smoke Playwright: 1M Ajustado 1120–1700, ±5 % 1049,5–1770,5, ±20 % 838–1982; persistencia OK; 0 errores de consola.

---

## [1.26.0-techo-suelo] - 2026-07-06

### Added
- 📏 **HV-039** El gráfico de precios marca el **techo** (máximo) y el **suelo** (mínimo) del rango con **líneas discontinuas** etiquetadas, y el eje Y deja **20 unidades de margen** por encima y por debajo (antes arrancaba pegado a los datos).

### Métricas
- 154 tests verdes (solo frontend). Smoke Playwright: 1M eje 1100–1720 (techo 1700/suelo 1120); 5D eje 1170–1590; 0 errores de consola.

---

## [1.25.0-eje-categorias] - 2026-07-06

### Changed
- 📈 **HV-038** El gráfico de precios usa un **eje de categorías** en vez de tiempo: en 5D/1M/… ya **no aparecen** los sábados/domingos, festivos ni noches (antes salían como huecos con una diagonal recta engañosa). Rangos diarios/semanales se agrupan por día UTC; el tooltip muestra la fecha/hora completa. El gráfico de valor de cuenta no cambia.

### Métricas
- 154 tests verdes (solo frontend). Smoke Playwright: eje `category`; `HY9H.F` 5D 124 pts saltando el finde (`03 jul`→`06 jul`), 1M 21 días hábiles, 1D 59 intradía; 0 errores de consola.

---

## [1.24.0-variacion-rango] - 2026-07-06

### Added
- 📊 **HV-037** El gráfico de precios muestra la **variación acumulada del rango** seleccionado (1D/5D/1M/…) en la cabecera (`#chartDelta`): importe en la **divisa del instrumento** y **% acumulado**, coloreado verde/rojo.

### Removed
- 🗑️ **HV-037** Eliminado el modo **"En vivo"** del gráfico (sin sentido desde HV-027) y los controles solo-vivo: "Gráfico cada" (`#refreshSelect`), "Histórico: N puntos" (`#historySelect`) y la cuenta atrás (`#tickCountdown`).

### Métricas
- 154 tests verdes (solo frontend, sin cambios de test). Smoke Playwright: sin "En vivo"; `HY9H.F` → `1M +210,00 € (+18,03 %)` verde, `5D −145,00 € (−9,54 %)` rojo, `1D 0,00 € (+0,00 %)` plano, 0 errores de consola. Yahoo activo.

---

## [1.23.2-td-error-claro] - 2026-07-06

### Changed
- 🔧 **HV-036** Con Twelve Data, el feed ya no loguea un `404` genérico con stack por símbolo cada ciclo: `TwelveDataProvider` lanza el **mensaje de Twelve Data** (p.ej. "available starting with the Grow plan") y el generador loguea solo el mensaje (una línea). Diagnóstico: el plan free de TD es solo US; los símbolos EU/Asia del usuario requieren plan de pago.

### Métricas
- 154 tests verdes (153 + 1). Smoke real: log limpio con el mensaje de TD (0 stacks).

---

## [1.23.1-fix-historico-404] - 2026-07-06

### Fixed
- 🐛 **HV-035** Al cambiar a Twelve Data, la app paraba con `HttpRequestException 404` al recargar el histórico de un símbolo estilo Yahoo (`^GSPC`/`HY9H.F`) que no existe en TD. Ahora los proveedores de histórico degradan a **serie vacía** ante 404/error (no lanzan) y `DashboardController.History` blinda con `catch` amplio. El gráfico muestra "sin datos" en vez de romper.

### Métricas
- 153 tests verdes (152 + 1). Smoke real: `^GSPC`/`HY9H.F` en TD → 200 vacío; `AAPL` → 100 puntos.

---

## [1.23.0-buscador-td] - 2026-07-06

### Added
- ✅ **HV-034** Buscador de instrumentos vía **Twelve Data** (`/symbol_search`) cuando es el proveedor activo. `TwelveDataInstrumentSearchProvider` + `SelectableInstrumentSearchProvider` (delega por proveedor activo). Permite dar de alta símbolos con la convención de TD (p.ej. SK hynix Frankfurt = `HY9H`).

### Métricas
- 152 tests verdes (149 + 3). Smoke real: buscador TD OK con key válida (en user-secrets).

---

## [1.22.0-selector-proveedor] - 2026-07-06

### Added
- ✅ **HV-033** **Selector de proveedor en la UI** (cambio en caliente Yahoo ↔ Twelve Data). `IMarketProviderState` conmutable + persistido (`App_Data/active-provider.txt`) + wrappers `Selectable*Provider` que delegan en el activo por llamada. `ProviderController` (`GET`/`POST /api/provider`). Selector en la cabecera con aviso "sin API key".

### Changed
- El proveedor ya no se fija en el arranque: se registran ambos concretos y el activo se resuelve en runtime. `DashboardService` reporta el proveedor activo (no el de config).

### Métricas
- 149 tests verdes (146 + 3). Smoke real: switch por API + Playwright del selector.

---

## [1.21.0-twelvedata-hist] - 2026-07-06

### Added
- ✅ **HV-032** Histórico vía **Twelve Data** (`/time_series`) cuando es el proveedor activo. `TwelveDataHistoryProvider : IMarketHistoryProvider` (precio+volumen, orden ascendente, mapa de rangos incl. YTD, normalización de símbolos cripto). El selector de DI (`useTwelveData`) unifica feed en vivo + histórico (Yahoo default | TwelveData).

### Métricas
- 146 tests verdes (142 + 4). Smoke real: histórico Yahoo default OK; arranque TwelveData sin errores de DI.

---

## [1.20.0-twelvedata] - 2026-07-06

### Added
- ✅ **HV-031** Segundo proveedor de datos en vivo: **Twelve Data**. `TwelveDataProvider : IMarketDataProvider` (endpoint `/quote`: precio+volumen+divisa) + `TwelveDataOptions` (BaseUrl/ApiKey/Timeout). Selector por `MarketData:ProviderType` ("TwelveData" | "YahooFinance" default) en DI. Badge de proveedor para Twelve Data. El histórico sigue en Yahoo.

### Métricas
- 142 tests verdes (137 + 5). Smoke real: selector Yahoo↔TwelveData por config; arranque OK.

### Notas
- Requiere API key de twelvedata.com (plan gratuito 8 req/min). `ApiKey` vacía en appsettings (no commitear con valor).

---

## [1.19.0-volumen-color] - 2026-07-06

### Changed
- ✅ **HV-030** Las barras de volumen se colorean por dirección: **verde** si el precio sube en la barra (alcista), **rojo** si baja (bajista). Convención financiera estándar (`backgroundColor` por barra).

### Métricas
- Build limpio (solo JS). Verificado con Playwright.

---

## [1.18.0-volumen] - 2026-07-06

### Added
- ✅ **HV-029** **Volumen de negociación** en el gráfico de precios (barras en eje secundario bajo el precio, LIVE e histórico). `PricePoint.Volume` + parseo de `indicators.quote[0].volume` en `YahooHistoryProvider` + volumen en la serie LIVE (`MarketTick.Volume`). Eje `yVol` oculto (~25% inferior), volumen fuera de leyenda, tooltip entero.

### Métricas
- 137 tests verdes (135 + 2). Verificado con Playwright (barras de volumen en 1D).

---

## [1.17.0-persist-range] - 2026-07-06

### Added
- ✅ **HV-028** El **rango del gráfico de precios** se persiste en `localStorage` (`chartRange`) y se restaura entre sesiones. Completa la persistencia de preferencias del panel (theme, chartSymbol, chartRefreshMs, historyPoints, accountRange, chartRange).

### Métricas
- Build limpio (solo JS). Verificado con Playwright (1D→5D→reload sigue en 5D).

---

## [1.16.0-chart-default-1d] - 2026-07-06

### Changed
- ✅ **HV-027** El gráfico de precios **arranca en 1D** (intradía real de Yahoo) en vez de "En vivo" (ticks locales dispersos). `chartMode` inicial `'1D'`, botón 1D activo, y el arranque carga el histórico tras el primer render. "En vivo" sigue disponible.

### Métricas
- Build limpio (solo JS/cshtml). Verificado con Playwright.

---

## [1.15.0-account-range] - 2026-07-06

### Added
- ✅ **HV-026** Selector de **rango temporal** (1 día / 1 semana / 1 mes / Todo) en el gráfico "Evolución del valor de cuenta". Densifica la vista reciente; la ventana se ancla al último snapshot (no a la hora actual). Cambio de rango sin refetch (desde caché), persistido en localStorage.

### Métricas
- Build limpio (solo JS/cshtml). Verificado con Playwright.

---

## [1.14.0-account-gaps] - 2026-07-06

### Fixed
- ✅ **HV-025** El gráfico "Evolución del valor de cuenta" ya no comprime el tramo reciente en un falso "pico": se aplica el corte de huecos (`insertGaps` genérico + `ACCOUNT_GAP_MS` 30 min + `spanGaps:false`) a las dos series. Resuelve el hallazgo #2 de la revisión con Playwright **sin borrar datos** (se inspeccionó `/api/account/history`: los snapshots eran legítimos, no había outlier).

### Métricas
- Build limpio (solo JS). Verificado con Playwright.

---

## [1.13.0-chart-gaps] - 2026-07-06

### Fixed
- ✅ **HV-024** El gráfico de precios "En vivo" ya no dibuja una diagonal recta engañosa a través de los huecos entre sesiones. `insertLiveGaps` inserta un punto nulo entre ticks con Δt > 5 min y `spanGaps:false` corta la línea. Solo en modo LIVE (el histórico de Yahoo es contiguo). Hallazgo de una revisión visual con Playwright.

### Notas
- Revisión visual del dashboard con Playwright (Chrome headless). Hallazgo abierto #2: el gráfico "Evolución del valor de cuenta" tiene una discontinuidad por mezclar snapshots pre-FX/pre-posiciones con los nuevos (se auto-corrige al acumular snapshots comparables).

---

## [1.12.0-fx-background] - 2026-06-29

### Added
- ✅ **HV-023** **Refrescar FX en background**. `IFxRateProvider.RefreshAsync` (fuerza fetch ignorando TTL) + `FxRefreshService : BackgroundService` que refresca las divisas en uso (watchlist + caja) cada `Fx:RefreshSeconds` (300). El dashboard ya no hace la llamada HTTP a la fuente FX en el hot path: lee de una caché siempre caliente.

### Métricas
- 135 tests verdes (132 + 3). Smoke real: arranque sin errores con el hosted service; dashboard OK.

---

## [1.11.0-pnl-base] - 2026-06-29

### Added
- ✅ **HV-022** **PnL convertido por fila**. `OpenTradeDto.UnrealizedPnLBase`/`ClosedTradeDto.RealizedPnLBase` (PnL × tipo del símbolo → base). UI: helper `pnlCell` muestra el equivalente en base (`≈ …`) cuando la divisa de la fila ≠ base.

### Métricas
- 132 tests verdes (131 + 1). Smoke real: campo presente (cartera all-EUR → base = nativo).

---

## [1.10.0-cash-fx] - 2026-06-29

### Added
- ✅ **HV-021** **Divisa por movimiento de caja**. `CashMovement.Currency` (default EUR) + migración `AddCashMovementCurrency`. `CashMovementDto`/`ICashService.AddAsync`/`CashController` aceptan divisa. `DashboardService` convierte el aportado neto por la divisa de cada movimiento. UI: campo Divisa en el alta + neto agrupado por divisa + importe por fila en su moneda.

### Métricas
- 131 tests verdes (129 + 2). Smoke real: alta 100 USD → netDeposits +87,67 (×0,8767) y limpieza OK.

---

## [1.9.0-fx] - 2026-06-29

### Added
- ✅ **HV-020** **Conversión FX a divisa base**. `IFxRateProvider`/`FxRateProvider` (Yahoo `{FROM}{TO}=X`, caché por par con TTL `Fx:CacheMinutes`, degradación a 1). `DashboardService` convierte los totales (invested/marketValue/realized/unrealized) a `Fx:BaseCurrency` (EUR) usando el tipo de cada símbolo; invariante `accountValue = netDeposits + totalPnL` preservado. `DashboardDto.BaseCurrency` + nota en UI.

### Fixed
- Los totales de cuenta ya **no mezclan divisas** (deuda dejada por HV-019). Las aportaciones de caja se asumen en divisa base.

### Métricas
- 129 tests verdes (123 + 6). Smoke real: fuente FX `USDEUR=X=0,8768`; dashboard `base=EUR`.

---

## [1.8.0-currency] - 2026-06-29

### Added
- ✅ **HV-019** **Divisa por instrumento**. `TrackedSymbol.Currency` + `SetCurrency` + migración `AddTrackedSymbolCurrency`. El proveedor devuelve `MarketQuote(Tick, Currency)` (Yahoo `meta.currency`) y el `MarketTickGeneratorService` sella la divisa de cada símbolo seguido cada ciclo. `Currency` en `TrackedSymbolDto`/`OpenTradeDto`/`ClosedTradeDto`. UI: columna "Div" + formateo monetario por fila en su divisa.

### Changed
- `IMarketDataProvider.GetLatestAsync` ahora devuelve `MarketQuote` en vez de `MarketTick` (contiene el tick + la divisa).

### Limitaciones conocidas
- Los **totales de cuenta** (valor de cuenta, efectivo, PnL) siguen sumándose en € y **mezclan divisas**. La conversión FX a una divisa base es el siguiente paso.

### Métricas
- 123 tests verdes (120 + 3). Smoke real: watchlist `HY9H.F→EUR`, `^GSPC→USD`; posición HY9H.F con divisa EUR.

---

## [1.7.0-csv-import] - 2026-06-29

### Added
- ✅ **HV-018** **Importar histórico de compras (CSV)**. `IPositionService.ImportCsvAsync` (reutiliza `OpenAsync`): parser con cabecera opcional, delimitador autodetectado `;`/`,` (con `;` admite coma decimal), fechas `yyyy-MM-dd`/`dd/MM/yyyy`/ISO (UTC); las filas con error se reportan por línea sin abortar el resto. `ImportResultDto`/`ImportErrorDto` + endpoint `POST /api/positions/import`. UI: modal "📥 Importar CSV" (subir fichero o pegar) con resultados.

### Métricas
- 120 tests verdes (116 + 4). Smoke real: endpoint responde con reporte de error por línea (fila inválida, sin escribir en BD).

---

## [1.6.0-account-history] - 2026-06-29

### Added
- ✅ **HV-017** **Histórico del valor de cuenta**. `PortfolioSnapshotService : BackgroundService` toma un snapshot del valor de cuenta al arrancar y cada `IntervalSeconds` (config `Snapshot`), persistiendo en la entidad `PortfolioSnapshot` (ya existente → **sin migración**). `AccountHistoryPointDto` + `IDashboardService.GetAccountHistoryAsync` (deriva `NetDeposits`/`ReturnPct`) + endpoint `GET /api/account/history?points=`. UI: tarjeta "📊 Evolución del valor de cuenta" con gráfico Chart.js (valor de cuenta + aportado neto), refresco propio cada 60 s.

### Métricas
- 116 tests verdes (113 + 5). Smoke real: arranque sin errores DI; `/api/account/history` → punto coherente (28.306,65 € / aportado 30.000 / −5,64 %).

---

## [1.5.0-pos-return] - 2026-06-29

### Added
- ✅ **HV-016** **Rentabilidad por posición**. `OpenTradeDto.ReturnPct` y `ClosedTradeDto.ReturnPct` calculados en `DashboardService` (`(precio−entry)/entry×100`, 0 % si entry=0). UI: nueva columna **"%"** tras PnL en las tablas de posiciones abiertas y trades cerrados, con signo y color. Métrica derivada, sin migración.

### Métricas
- 113 tests verdes (111 + 2: % abierta +10 %, % cerrada −10 %). Smoke real manual pendiente.

---

## [1.4.0-return] - 2026-06-29

### Added
- ✅ **HV-015** **Rentabilidad porcentual de la cuenta**. `DashboardDto.ReturnPct` + cálculo en `DashboardService`: `ReturnPct = TotalPnL / NetDeposits × 100` (0 % si `NetDeposits = 0`; equivale a `(AccountValue − NetDeposits) / NetDeposits × 100` por el invariante de HV-014). Métrica derivada, sin migración ni cambios de dominio/BD.
- UI: el subtítulo de la tarjeta **Valor de cuenta** muestra el **% con signo y color** (verde/rojo) + "sobre aportado", reutilizando `pctSigned`/`setSigned`. Se mantienen 6 tarjetas.

### Métricas
- 111 tests verdes (108 + 3: ganancia +0,5 %, pérdida −10 %, sin aportaciones → 0 %). Smoke real manual pendiente.

---

## [1.3.0-cash] - 2026-06-16

### Added
- ✅ **HV-014** Concepto de **caja/efectivo** y **valor de cuenta**. Entidad `CashMovement` (ingresos/retiradas) + migración `AddCashMovement` + `CashService` + endpoints `GET/POST/DELETE /api/cash`. `DashboardService` deriva `NetDeposits`, `Cash` (= aportado − invertido + realizado) y `AccountValue` (= efectivo + cartera). Invariante: valor de cuenta = aportado + PnL total.
- UI: métricas reorganizadas a **6 tarjetas** (Valor de cuenta · Efectivo · Valor de cartera · PnL total · Posiciones · Winrate) + **modal "💰 Caja"** (alta Ingreso/Retirada + lista con borrado).

### Métricas
- 108 tests verdes. Smoke real: aporta 10 000 € + HY9H.F 1360×5 → efectivo 3 200 €, valor de cuenta 10 150 € (= 10 000 + PnL 150).

---

## [1.2.0-portfolio] - 2026-06-16

### Added
- ✅ **HV-013** Tracker de **cartera real**: alta manual de posiciones (objetivo: dejar TradingView). `IPositionService`/`PositionService` (open/close/delete por Id, varias posiciones por símbolo, auto-añade el símbolo a la watchlist) + endpoints `POST /api/positions`, `POST /api/positions/{id}/close`, `DELETE /api/positions/{id}`.
- **Modal "➕ Nueva posición"** (símbolo con datalist de la watchlist, precio de entrada, cantidad, fecha) y columna **Acciones** (Cerrar / eliminar) en la tabla de posiciones abiertas. PnL no realizado en vivo contra el precio real.
- El **buscador** pasó a ser una **modal** (botón "🔎 Buscar / añadir instrumento"; se cierra al añadir).

### Changed
- **Capital derivado de las posiciones reales**: se elimina el `CapitalInicial` fijo (10 000 €) y su `PortfolioOptions`/config. `DashboardDto` expone `Invested` (coste base de abiertas) y `MarketValue` (valor a precio real); la tarjeta principal muestra "Valor de cartera" + "Invertido". `TotalPnL` = realizado + no realizado.

### Métricas
- 103 tests verdes. Smoke real: HY9H.F 1360×5 → Invertido 6800 €, Valor de cartera ~6975 € (precio real ~1395), PnL +175 €.

---

## [1.1.0-tracker] - 2026-06-16

Pivote de **simulador** a **tracker de precios reales**.

### Added
- ✅ **HV-010** Buscador de instrumentos por descripción / ISIN / ticker. `IInstrumentSearchProvider` + `YahooInstrumentSearchProvider` (`/v1/finance/search`), endpoint `GET /api/instruments/search`, tarjeta de búsqueda + tabla en el dashboard. (WKN no soportado por Yahoo → fuera de alcance.)
- ✅ **HV-011** Watchlist persistida y editable en runtime. Entidad `TrackedSymbol` + migración `AddTrackedSymbol` + `IWatchlistService`. El generador lee los símbolos de BD en cada ciclo. Endpoints `GET/POST/DELETE /api/instruments/track[ed]`. Botones "+ Añadir" (resultados) y "✕" (quitar) en el panel. Seed inicial `HY9H.F` (SK hynix Inc., Frankfurt).

### Changed
- Feed en vivo 100% **datos reales** de Yahoo; intervalo de sondeo a 30 s. `MarketDataOptions`/`appsettings` reducidos (sin `Symbols`/`Volatility`/`Drift`/`Volume`/`Seed`; los símbolos viven en `TrackedSymbol`).

### Removed
- ✅ **HV-012** Retirada la simulación `RandomWalkTickGenerator` (+ su test). Estrategia automática **MA Crossover desactivada** (panel solo-visor; código conservado para reactivar).

### Métricas
- 96 tests verdes (build con `TreatWarningsAsErrors`).
- Smoke test real: arranque OK, `provider=YahooFinance`, tick real `HY9H.F`=1390 €; alta/baja de símbolos verificada por endpoints.

### Incidencias
- ⚠️ `ESTADO_PROYECTO.json` apareció a **0 bytes** durante la sesión (causa externa, no edición de Claude). Reconstruido desde el import de `CLAUDE.md` con los 3 evolutivos nuevos.

---

## [1.0.0-MVP] - 2026-05-26

### Completado

**MVP del AI Trading Simulator** entregado en una sesión:

- ✅ HV-001 Scaffold Clean Architecture (5 proyectos .NET 10)
- ✅ HV-002 Modelo de dominio (Trade, MarketTick, PortfolioSnapshot, TradeStatus, TradeSignal)
- ✅ HV-003 Persistencia EF Core + SQLite + migrations
- ✅ HV-004 BackgroundService generador random-walk de ticks
- ✅ HV-005 Estrategia MA Crossover automática + ChannelTickBus + OrderService
- ✅ HV-006 Dashboard con métricas y gráfico Chart.js

**Métricas finales:**
- 78 tests unitarios e integración verdes
- 6 evolutivos cerrados
- App funcional: `dotnet run` → `https://localhost:5099/`
- Smoke test runtime: dashboard muestra PnL no realizado en vivo, trades, gráfico

## [Unreleased] - 2026-06-15

### Fixed

- **HV-008** Fix `YahooFinanceProvider` — el endpoint `/v7/finance/quote` empezó a devolver **401** (Yahoo exige cookie+crumb). Detectado en smoke test real (pendiente desde HV-007; los tests mockeados no lo cubrían).
  - Migrado a `GET /v8/finance/chart/{symbol}?interval=1d&range=1d`, que sirve precio + volumen sin auth. Parseo de `chart.result[0].meta`.
  - User-Agent de navegador en el HttpClient `YahooFinance` (`TryAddWithoutValidation`).
  - `YahooFinanceProviderTests` reescritos al shape `/v8/chart` (mismos casos, 11 tests verdes).
  - **Smoke test real ✅**: AAPL=291,13 (vol 37.905.580), GOOG=358,16 (vol 17.657.657). `BTCUSD` → 404 capturado per-símbolo (usar `BTC-USD` para Yahoo).
  - **89 tests verdes**. Build verde con `TreatWarningsAsErrors=true`.

- **Dashboard (Chart.js)** — corregido crash JS en `PointElement.inRange` al hacer hover sobre el gráfico (detectado en smoke test).
  - `wwwroot/js/dashboard.js`: `priceChart.update('none')` → `priceChart.update()` + `animation: false` (el modo `'none'` dejaba los `PointElement` nuevos con `options=undefined`).
  - Blindaje defensivo de puntos: `x` como epoch numérico + `.filter(Number.isFinite)` en `x`/`y`.
  - Lección registrada como **L-001** en `_duran/LECCIONES.md`.

### Changed (UI dashboard)

- **Layout a ancho completo**: `_Layout.cshtml` usa `container-fluid` (nav, main, footer). `lang="es"`, título y brand → "🤖 AI Trading Simulator" enlazando al Dashboard.
- **Modo oscuro** (Bootstrap 5.3 `data-bs-theme`): toggle en navbar + persistencia en `localStorage` + script anti-parpadeo en `<head>`. Navbar adaptable (`bg-body-tertiary`).
- **Gráfico de precios en valor real con eje a la derecha** (estilo trading): eje Y `position: 'right'` con formato de precio, margen derecho (~12% extra en el `max` del eje X) para que la línea no quede pegada al borde, y plugin `currentValueLabels` que dibuja el último valor de cada serie como etiqueta coloreada **sobre el propio eje derecho** (estilo TradingView), con una flechita que apunta al nivel del precio. (Sustituye la normalización a % de cambio previa.)
- **Selector de símbolo del gráfico**: `<select>` que se autopobla con las series disponibles (+ "Todos"), persistido en `localStorage` (`chartSymbol`). Por defecto muestra un único símbolo (escala legible); evita el aplastamiento por mezclar magnitudes (BTC vs acciones). Cambiarlo re-renderiza y reescala al instante.
- **Barra de rango temporal con histórico real (HV-009)**: botones `En vivo · 1D · 5D · 1M · 3M · 6M · YTD · 1A · 3A · 5A` bajo el gráfico (`#rangeBar`). Al elegir un rango se cargan **OHLC reales de Yahoo** vía `GET /api/history?symbol&range` (`IMarketHistoryProvider`/`YahooHistoryProvider` sobre `/v8/chart` con `range`/`interval`, normaliza `BTCUSD→BTC-USD`); "En vivo" vuelve a la simulación. Eje X con unidad temporal adaptativa (segundos…años). El HttpClient Yahoo se registra siempre. Modo histórico pausa el redibujado en vivo del gráfico (métricas/tablas siguen en vivo).
- **Cuenta atrás al próximo refresco del gráfico**: overlay (`#tickCountdown`) sobre el gráfico, justo debajo de la etiqueta de valor de la serie activa (`⏱ X.Xs`). Es **relativa al selector "Gráfico cada"** (`chartIntervalMs`): cuenta `lastChartAt + chartIntervalMs - now`, así que se reinicia exactamente en cada redibujo y refleja el intervalo elegido (3 s…5 min). Solo en modo "En vivo"; se actualiza cada 150 ms vía overlay HTML (sin repintar el canvas); posición con `scales.y.getPixelForValue`.
- **Rejilla del gráfico visible en ambos temas**: `grid.color` y `ticks.color` como funciones scriptables que leen `data-bs-theme` → la cuadrícula y los números del eje se adaptan a claro/oscuro (antes la rejilla negra translúcida era invisible en modo oscuro).
- **Selector de histórico**: `<select>` (50 / 250 / 1.000 / Máx 5.000 puntos) persistido en `localStorage` (`historyPoints`). El endpoint `/api/dashboard/data` acepta `?points=N` (acotado 10–5.000) → `DashboardService.GetSnapshotAsync(priceSeriesPoints)`. Los ticks ya se persistían en `App_Data/trading.db` (no se borran al arrancar); esto hace **visible** el histórico acumulado entre sesiones en vez de la ventana corta de 50 puntos.
- **Formato es-ES**: capital/PnL como moneda (`Intl.NumberFormat` EUR), precios con 2-4 decimales, PnL coloreado verde/rojo.
- **Estado del feed** en la cabecera: badge del proveedor activo (RandomWalk/Yahoo), nº de ticks y hora del último tick ("en vivo" con punto pulsante). Backend: `DashboardDto` + `DashboardService` exponen `ProviderType`, `TotalTicks`, `LastTickUtc` (nuevo campo `ProviderType` en `MarketDataOptions`).
- **Cards y tablas**: cards de métricas con acento Comillas (`#0066cc`) + hover, tablas con scroll y cabecera fija, paleta corporativa en `site.css`.
- **Refresco desacoplado**: métricas, estado y tablas siempre en vivo (3 s); el **gráfico** se refresca según un selector propio en la cabecera (Tiempo real 3 s / 30 s / 1 min / 2 min / 5 min), persistido en `localStorage` (`chartRefreshMs`). Implementado con una sola petición por ciclo + compuerta temporal (`lastChartAt`); cambiar el selector fuerza un redibujado inmediato del gráfico.

### Removed

- Código muerto de la plantilla por defecto: `HomeController.cs`, `Views/Home/Index.cshtml`, `Views/Home/Privacy.cshtml` y los enlaces Home/Privacy de navbar y footer.

### Added

- **Retención de la BD por tamaño** (`DatabaseRetentionService : BackgroundService`): cuando el fichero supera `Retention:MaxDatabaseSizeMb` (default **1024 MB = 1 GB**), purga los `MarketTick` más antiguos hasta `LowWaterMarkFraction` del límite (default 0,8) y ejecuta `VACUUM` para reclamar espacio. Configurable: `Enabled`, `MaxDatabaseSizeMb`, `CheckIntervalSeconds` (default 300), `LowWaterMarkFraction`. Opciones en `RetentionOptions` + sección `Retention` de `appsettings.json`. Tamaño medido con `PRAGMA page_count × page_size` (excluye WAL transitorio, refleja el VACUUM), expuesto vía `ITradingDbContext.GetDatabaseSizeBytesAsync` (reutilizado por el servicio de retención y el dashboard). Verificado end-to-end forzando el límite a 1 MB con ticks rápidos. **89 tests verdes**.
- **Indicador de tamaño de BD en el dashboard**: `DashboardDto.DatabaseSizeBytes` + elemento "BD:" en la cabecera (junto a "Ticks"), formateado (KB/MB) — permite ver de un vistazo cuánto pesa y cómo actúa la retención.

## [Unreleased] - 2026-05-26

### Added (Fase 2 iniciada)

- **HV-007** Provider real Yahoo Finance + abstracción intercambiable
  - `Application/Common/Options/YahooFinanceOptions.cs` (BaseUrl + TimeoutSeconds)
  - `Infrastructure/MarketData/YahooFinanceProvider.cs` (HttpClient + JSON parsing + validación defensiva)
  - Helpers de test reutilizables en `Tests/Helpers/`: `MockHttpMessageHandler`, `TestHttpClientFactory`
  - 11 tests nuevos `YahooFinanceProviderTests` (success, sin result, precio cero/null, HTTP 4xx/5xx, JSON malformado, volume null, endpoint, símbolos especiales)
  - Paquete `Microsoft.Extensions.Http` 10.0.8 añadido a Infrastructure
  - `Web/appsettings.json` con `MarketData.ProviderType` (default "RandomWalk") y subsección `MarketData.YahooFinance`
  - **89 tests verdes total**

### Changed (refactor para Fase 2)

- `IMarketDataProvider` ahora es async: `Task<MarketTick> GetLatestAsync(symbol, t, ct)`
- `RandomWalkTickGenerator` adaptado a la nueva firma con `Task.FromResult(tick)`
- `MarketTickGeneratorService.GenerateAndPersistAsync` ahora await + try/catch por símbolo individual (resiliente a fallos de providers externos)
- `Infrastructure/DependencyInjection.cs`: extraído `RegisterMarketDataProvider` con switch por `MarketData:ProviderType` (RandomWalk default, YahooFinance opcional)
- 8 tests `RandomWalkTickGeneratorTests` migrados a `async Task` con `await GetLatestAsync(...)`

### Arquitectura

- **Patrón "switch en DI por config"**: para añadir AlphaVantage o Binance mañana, basta 1 archivo nuevo (provider) + 1 case en el switch. Cero cambios en `MarketTickGeneratorService`, estrategia, dashboard o tests del bus.

- **HV-006** Dashboard con métricas y gráfico Chart.js
  - `Application/Common/Options/PortfolioOptions.cs` (CapitalInicial 10000)
  - `Application/Common/Dtos/DashboardDto.cs` + 4 records (OpenTradeDto, ClosedTradeDto, PriceSeriesDto, PricePoint)
  - `Application/Common/Interfaces/IDashboardService.cs` + `Application/Services/DashboardService.cs`
  - `Web/Controllers/DashboardController.cs` con `Index()` + `/api/dashboard/data`
  - `Web/Views/Dashboard/Index.cshtml` (Bootstrap 5 cards + tablas + canvas)
  - `Web/wwwroot/js/dashboard.js` (polling 3s + Chart.js)
  - `Web/Program.cs`: default route → Dashboard + `AddJsonOptions(CamelCase)` explícito
  - `Web/appsettings.json`: sección `Portfolio`
  - Chart.js 4.4.0 + chartjs-adapter-date-fns 3.0.0 via CDN
  - 7 tests `DashboardServiceTests` con SQLite `:memory:`
  - **Smoke test runtime ✅**: `/api/dashboard/data` devuelve JSON correcto con `unrealizedPnL` calculado en vivo

### Changed

- `Infrastructure/DependencyInjection.cs`: + `PortfolioOptions` Options + `IDashboardService` Scoped
- `DashboardService` calcula agregados en memoria (no en SQL) para evitar bug de SQLite+decimal-TEXT

### Fixed

- Detectado y mitigado: queries LINQ con aritmética sobre `decimal` mapeado como TEXT en SQLite devuelven 0. Workaround: `ToListAsync()` + agregar en C#. Documentado como DT-PERF-001 para futura migración.

- **HV-005** Estrategia MA Crossover automática (cerebro del simulador)
  - `Domain/Enums/TradeSignal.cs` (Buy=0, Sell=1)
  - `Application/Common/Interfaces/ITickBus.cs` + `Application/Common/Interfaces/IOrderService.cs`
  - `Application/Common/Options/StrategyOptions.cs` (ShortWindow=5, LongWindow=20, Quantity=1.0, validación constructor Short<Long)
  - `Application/Strategies/IStrategy.cs` + `MovingAverageCrossoverStrategy.cs` (rolling windows por símbolo, detección de cruce por cambio de estado booleano)
  - `Application/Services/OrderService.cs` (regla 1 posición abierta por símbolo)
  - `Infrastructure/MarketData/ChannelTickBus.cs` (Channel<MarketTick> bounded 1000, DropOldest, SingleReader)
  - `Infrastructure/Strategy/StrategyExecutionService.cs` (BackgroundService que consume bus y delega en IOrderService)
  - Refactor `MarketTickGeneratorService`: batch atómica → persist → publish al bus
  - 21 tests nuevos: 11 estrategia + 6 OrderService + 3 ChannelTickBus + 1 boundary case
  - **Smoke test runtime ✅**: 147 MarketTicks + 1 Trade BUY abierto en 95s ejecutándose
  - **71 tests verdes en total**

### Changed

- `Infrastructure/MarketData/MarketTickGeneratorService.cs` ahora inyecta `ITickBus` y publica cada tick tras persistir exitosamente
- `Infrastructure/DependencyInjection.cs`: añadidos 5 registros (StrategyOptions + OrderService Scoped + ChannelTickBus Singleton + MovingAverageCrossoverStrategy Singleton + StrategyExecutionService HostedService)
- `Web/appsettings.json`: sección `Strategy`

### Added (previo)

- **HV-004** BackgroundService generador de ticks de mercado
  - `Application/Common/Interfaces/IMarketDataProvider.cs` — contrato de generación de ticks
  - `Application/Common/Options/MarketDataOptions.cs` — config (símbolos+precio inicial, intervalo, volatilidad, drift, volumen, seed)
  - `Infrastructure/MarketData/RandomWalkTickGenerator.cs` — random walk geométrico con Box-Muller para muestreo Normal(0,1), thread-safe via `lock` sobre `Random`, seedable para tests
  - `Infrastructure/MarketData/MarketTickGeneratorService.cs` — `BackgroundService` con `PeriodicTimer(TimeProvider)` + `IServiceScopeFactory` para crear scope por ciclo
  - `appsettings.json` con sección `MarketData` (3 símbolos: AAPL=175, GOOG=140, BTCUSD=65000; intervalo 2s; volatilidad 0.3%)
  - Paquetes NuGet añadidos: `Microsoft.Extensions.Hosting.Abstractions` 10.0.8, `Microsoft.Extensions.Options.ConfigurationExtensions` 10.0.8
  - Logging: `Microsoft.EntityFrameworkCore` bajado a `Warning` para no spammear con cada INSERT
  - 8 tests del generador (`RandomWalkTickGeneratorTests`): determinismo con seed, no-negatividad bajo volatilidad extrema, símbolos configurados, normalización, volumen en rango, independencia entre símbolos
  - **Smoke test runtime ✅**: app ejecutándose 12s genera 12 ticks (4 por símbolo × 3 símbolos), precios coherentes con iniciales, volúmenes en rango
  - **50 tests verdes** (42 + 8 nuevos)

- **HV-003** Persistencia EF Core 10.0.8 + SQLite
  - Paquetes EF Core añadidos: `EntityFrameworkCore` (Application), `EntityFrameworkCore.Sqlite` (Infrastructure, Tests), `EntityFrameworkCore.Design` (Web)
  - `Application/Common/Interfaces/ITradingDbContext.cs` — contrato con 3 DbSets + SaveChangesAsync
  - `Infrastructure/Persistence/TradingDbContext.cs` (sealed)
  - 3 entity configurations: `Trade`, `MarketTick`, `PortfolioSnapshot` con `decimal` mapeado a `TEXT` (precisión preservada en SQLite)
  - `Infrastructure/DependencyInjection.cs` con extension `AddInfrastructure(IConfiguration)`
  - `Web/Program.cs` invoca `AddInfrastructure` y aplica migrations al arrancar (`MigrateAsync`)
  - `Web/appsettings.json` con `ConnectionStrings:Default = Data Source=App_Data/trading.db`
  - Migration inicial `20260526135749_InitialCreate` con CREATE TABLE para 3 tablas + 4 índices
  - Tool global `dotnet-ef` 10.0.8 instalada
  - 6 tests de integración (`Tests/Infrastructure/TradingDbContextTests.cs`) con SQLite `:memory:`: round trip por entidad + preservación de precisión decimal (caso cripto 8 decimales) + filtrado indexado + estado Open tras reload
  - Smoke test runtime: `dotnet run` aplica migration y crea `App_Data/trading.db` (+ WAL files) sin errores
  - **42 tests verdes** (36 Domain + 6 Infrastructure)

- **HV-002** Modelo de dominio puro (Clean Architecture - Domain layer)
  - `Domain/Enums/TradeStatus.cs` (Open=0, Closed=1)
  - `Domain/Entities/Trade.cs` (factory `Open`, método `Close`, `RealizedPnL`, invariantes en factories)
  - `Domain/Entities/MarketTick.cs` (inmutable, factory `Create`, invariantes Price/Volume)
  - `Domain/Entities/PortfolioSnapshot.cs` (factory `Create`, `TotalPnL`, contadores no-negativos)
  - Suite de tests: `TradeTests` (12 métodos / ~22 casos con [Theory]), `MarketTickTests` (6 / ~10), `PortfolioSnapshotTests` (7)
  - **36 tests verdes**, 0 fallos
  - Domain sigue **sin paquetes NuGet** (solo BCL)
  - Eliminados placeholders `Domain/Class1.cs` y `Tests/UnitTest1.cs`

- **HV-001** Scaffold inicial Clean Architecture (.NET 10)
  - Solución `Comillas.AITradingSimulator.slnx` (formato XML slnx por defecto en SDK 10.0.300)
  - 5 proyectos: `Domain` (classlib), `Application` (classlib), `Infrastructure` (classlib), `Web` (mvc), `Tests` (xunit)
  - Referencias entre proyectos respetando Clean Architecture (Domain sin deps, Application → Domain, Infrastructure → Application+Domain, Web → Application+Infrastructure, Tests → todos)
  - `TreatWarningsAsErrors=true` en los 5 csproj (build verde con 0 warnings)
  - `App_Data/.gitkeep` en Web (placeholder para futura `trading.db`)
  - `.gitignore` raíz cubriendo `bin/`, `obj/`, `*.db`, `App_Data/*.db*`, `.vs/`, secrets, STIC.IA download, MCP credentials
  - Visual Studio Solution Folders configuradas vía `integracion-vs.ps1` (89 carpetas, 306 archivos)
- Onboarding completado con DURAN configurado para "AI Trading Simulator" (proyecto personal, .NET 10, MVC + Clean Architecture + SQLite)
- Spec `_duran/specs/HV-001.md`

### Verificación

- `dotnet build`: 0 errores, 0 warnings (con `TreatWarningsAsErrors=true`)
- `dotnet test`: 1 test placeholder verde (xunit default)
- SDK utilizado: .NET 10.0.300

## [Unreleased]

### Added
-

### Changed
-

### Fixed
-

---

## [X.Y.Z] - YYYY-MM-DD

### Resumen
[Breve descripcion de esta version]

### Added
- [Funcionalidad nueva] (#issue) - @desarrollador

### Changed
- [Cambio realizado] (#issue) - @desarrollador

### Fixed
- [Bug corregido] (#issue) - @desarrollador

### Notas de la Version
- [Cualquier nota relevante para esta version]
- [Instrucciones especiales de migracion si aplica]

---

## Plantilla para Nueva Version

```markdown
## [X.Y.Z] - YYYY-MM-DD

### Resumen
[Descripcion breve]

### Added
-

### Changed
-

### Deprecated
-

### Removed
-

### Fixed
-

### Security
-

### Notas de la Version
-
```

---

## Historial de Versiones

| Version | Fecha | Tipo | Descripcion |
|---------|-------|------|-------------|
| X.Y.Z | YYYY-MM-DD | [Major/Minor/Patch] | [Descripcion breve] |

---

**Ultima actualizacion**: [YYYY-MM-DD]
