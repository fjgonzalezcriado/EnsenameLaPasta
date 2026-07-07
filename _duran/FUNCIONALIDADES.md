# Catalogo de Funcionalidades - AI Trading Simulator

> Simulador de trading algoritmico personal. Fuente de verdad: `00_Gestion/Requerimientos/REQUERIMIENTOS.md`.

---

## 🟡 En Progreso

_Vacío. La app evolucionó de simulador a **tracker de precios reales**: buscador de instrumentos + watchlist editable + feed 100% Yahoo (sin simulación ni auto-trading). Próximos candidatos: AlphaVantage/Binance, P&L de holding real (posición vs entrada), dashboard "visor puro"._

---

## ✅ Completados

#### HV-048 Métricas avanzadas de cartera (drawdown, Sharpe, volatilidad, profit factor) ✅
- **Estado**: ✅ Completado · **Período**: 2026-07-07 · **Resultado**: ✅ Cumplido · **Tipo**: Evolutivo
- **Spec**: `_duran/specs/HV-048.md`
- **Resumen**: Tarjeta "📐 Métricas avanzadas" con **Max Drawdown** (+ actual), **Sharpe**, **volatilidad** anualizados y **Profit Factor**. `DashboardService.GetPortfolioMetricsAsync` + `GET /api/account/metrics`. Las métricas de rendimiento se calculan sobre el **índice de retorno** (`1 + PnL/aportado`), **neutral a aportaciones/retiradas** (un ingreso de caja no cuenta como rentabilidad); drawdown pico→valle del índice; Sharpe/vol sobre retornos diarios anualizados √252 con **gate de honestidad ≥ 10 retornos diarios** (con menos → "insuficiente", para no anualizar muestras minúsculas). Profit Factor de trades cerrados (∞ si solo ganancias). 4 tests nuevos. 185 verdes. Smoke real: Max Drawdown −7,69 % mostrado; Sharpe/vol "necesitan ≥ 11 días (hay 3)". **Corrección**: el 1er intento sobre valor de cuenta bruto daba Sharpe 12,56/vol 143 % por las aportaciones → corregido con el índice de retorno.

#### HV-047 Importar trades cerrados por CSV ✅
- **Estado**: ✅ Completado · **Período**: 2026-07-07 · **Resultado**: ✅ Cumplido · **Tipo**: Evolutivo
- **Spec**: `_duran/specs/HV-047.md`
- **Resumen**: Amplía el import de HV-018 (que solo abría posiciones) para importar también **trades cerrados**. `ImportCsvAsync` acepta `symbol, entry, qty[, date[, exit[, closeDate]]]`: si la fila trae **`exit`** se abre y se cierra (`CloseAsync(exit, closeDate ?? openedAt)`); si no, posición abierta. Pre-validación por fila (exit inválido / closeDate sin exit / cierre anterior a apertura → error **sin dejar la posición a medio abrir**). Reutiliza el parser (cabecera/delimitador/coma decimal/fechas). Modal actualizado con el nuevo formato y ejemplo. Solo Application, sin migración. 3 tests nuevos. 181 verdes. Smoke (sin tocar la cartera real): errores reportados correctamente; camino feliz cubierto por tests unitarios (estado Closed + PnL 150 + fecha).

#### HV-046 Dashboard visor puro (modo solo lectura) ✅
- **Estado**: ✅ Completado · **Período**: 2026-07-07 · **Resultado**: ✅ Cumplido · **Tipo**: Evolutivo
- **Spec**: `_duran/specs/HV-046.md`
- **Resumen**: Botón **"👁 Visor"** que pone el panel en **solo lectura** (ideal para mostrar en pantalla). Añade `viewer-mode` al contenedor y oculta vía CSS (`.js-editor-only`) los controles que mutan datos: selector de proveedor, "Buscar/añadir", "Caja", "Importar CSV", "Nueva posición", el "✕" de quitar símbolo y la columna "Acciones" (`renderOpenTrades` no la dibuja y ajusta el colspan). Se **conservan** los controles de vista (rango, símbolo, velas, señales ML, margen) y toda la visualización. Persistido en `localStorage`. Solo frontend. 178 verdes (sin cambios). Smoke Playwright: OFF muestra todo, ON oculta la edición, persiste tras reload, 0 errores.

#### HV-045 ML — más features + segundo modelo (FastTree) con selección ✅
- **Estado**: ✅ Completado · **Período**: 2026-07-07 · **Resultado**: ✅ Cumplido · **Tipo**: Evolutivo (Fase 3)
- **Spec**: `_duran/specs/HV-045.md`
- **Resumen**: Amplía el clasificador de HV-044. **Features 7 → 12**: añade momentum 10p, precio vs SMA20, **MACD** (histograma normalizado), **Bollinger %B**(20) y **estocástico %K**(14) — EMAs del MACD precomputadas; warmup `Lookback=26`, `MinPoints=60`. **Segundo modelo**: además de SDCA se entrena **FastTree** (`Microsoft.ML.FastTree`, determinista) y se **elige el de mayor AUC** en el hold-out (accuracy de desempate). `DirectionSignal.ModelUsed` reportado; el tooltip de la UI muestra modelo + features + accuracy/AUC. 178 verdes. Smoke real: `HY9H.F 6M → SDCA` (AUC 0,65), `MBOT 1A → FastTree` — distintos símbolos eligen distinto modelo. **Honestidad**: calidad 0,50–0,65 (nivel azar-ish) reportada con transparencia; indicador, no asesoramiento.

#### HV-044 ML.NET — clasificación sube/baja (SDCA + features técnicas) ✅
- **Estado**: ✅ Completado · **Período**: 2026-07-07 · **Resultado**: ✅ Cumplido · **Tipo**: Evolutivo (Fase 3)
- **Spec**: `_duran/specs/HV-044.md`
- **Resumen**: Amplía ML.NET con un **clasificador binario de dirección** (¿sube el próximo periodo?). `IDirectionClassifier`/`MlDirectionClassifier` (regresión logística **SDCA**) con **7 features técnicas** (retorno, momentum 5p, ratio de medias, precio vs SMA, **RSI(14)**, volatilidad, volumen relativo); etiqueta `close[i+1]>close[i]`; **split cronológico 80/20** + evaluación honesta (accuracy/AUC en hold-out). Predice el último dato → **señal** Comprar/Vender/Mantener + P(sube). `GET /api/signal`. UI: el switch pasa a **"🔮 Señales ML"** y muestra dos badges — 🔮 pronóstico (SSA) + 📊 clasificación, con la **calidad del modelo en el tooltip**. Guardas: mín. 40 puntos, ambas clases en train, degrada sin lanzar. 3 tests nuevos. 178 verdes. Smoke real (Yahoo): `HY9H.F 6M` → Comprar (Sube 68%), accuracy 0,61 / AUC 0,64 (n=88). **Honestidad**: la dirección de precio ronda el azar; indicador educativo, no asesoramiento.

#### HV-043 Señales con ML.NET — pronóstico de precio (SSA) ✅
- **Estado**: ✅ Completado · **Período**: 2026-07-07 · **Resultado**: ✅ Cumplido · **Tipo**: Evolutivo (Fase 3)
- **Spec**: `_duran/specs/HV-043.md`
- **Resumen**: Primer uso de **ML.NET** (`Microsoft.ML.TimeSeries` 4.0.2): pronóstico de precio con **SSA** sobre los cierres del histórico del símbolo. `IPriceForecaster`/`SsaPriceForecaster` (entrena al vuelo con `IMarketHistoryProvider`, `ForecastBySsa` horizonte N + banda de confianza 95 %) → deriva **señal** Alcista/Bajista/Neutral (umbral ±0,5 %). `GET /api/forecast`. UI: switch **"🔮 Pronóstico"** + badge de señal + overlay en el gráfico (línea naranja discontinua + banda sombreada sobre categorías futuras +1..+N). Degrada sin lanzar (mín. 12 puntos; símbolo inválido → "Insuficiente"). Añade `Directory.Build.props` que suprime un advisory NuGet **transitivo de SQLite** (preexistente, destapado por el restore en fresco de ML.NET). 4 tests nuevos. 175 verdes. Smoke real (Yahoo): `HY9H.F 6M` → Alcista +39 % con banda (screenshot OK); degradación limpia. **Caveat**: SSA en series cortas extrapola con fuerza (la banda refleja la incertidumbre); no es asesoramiento financiero.

#### HV-042 Tercer proveedor de datos — Alpha Vantage ✅
- **Estado**: ✅ Completado · **Período**: 2026-07-07 · **Resultado**: ✅ Cumplido · **Tipo**: Evolutivo (Fase 3)
- **Spec**: `_duran/specs/HV-042.md`
- **Resumen**: Añade **Alpha Vantage** como 3er proveedor conmutable, reutilizando el patrón de Twelve Data. `AlphaVantageOptions` + `AlphaVantageProvider` (`GLOBAL_QUOTE`: precio+volumen; sin divisa; mensaje claro en cuota/error — AV responde 200 con `Note`/`Information`/`Error Message`) + `AlphaVantageHistoryProvider` (`TIME_SERIES_INTRADAY/DAILY/WEEKLY`, clave de serie localizada dinámicamente, OHLC, recorte a N recientes, YTD por año; degrada a vacío sin key) + `AlphaVantageInstrumentSearchProvider` (`SYMBOL_SEARCH`). `MarketProviderState`/wrappers `Selectable*`/DI extendidos; `ProviderController` expone `alphaVantageKeyConfigured`; opción "Alpha Vantage" + aviso "⚠ sin API key" en la UI. 15 tests nuevos (+1 adaptado). 171 verdes. Smoke de cableado (sin key): switch refleja en dashboard, histórico/búsqueda degradan a vacío (0 stacks), POST inválido→400, selector con 3 opciones. **Limitación**: plan gratuito de AV = 25 req/día (insuficiente para el feed en vivo). Key en user-secrets (no en repo).

#### HV-041 Switch de gráfico línea ↔ velas japonesas (OHLC) ✅
- **Estado**: ✅ Completado · **Período**: 2026-07-06 · **Resultado**: ✅ Cumplido · **Tipo**: Evolutivo
- **Spec**: `_duran/specs/HV-041.md`
- **Resumen**: Nuevo **switch "🕯️ Velas"** para alternar el gráfico de precios entre **lineal** y **velas japonesas** (OHLC). Backend: `PricePoint` gana `Open/High/Low` (default 0; `Price`=cierre) y `YahooHistoryProvider`/`TwelveDataHistoryProvider` parsean OHLC (fallback al cierre si falta). Frontend: **plugin propio `candlePlugin`** dibuja mecha (máx→mín) + cuerpo (apertura↔cierre) verde/rojo por categoría — sin librería externa, respetando el eje de categorías de HV-038 (sin huecos de findes). En velas la línea se oculta, techo/suelo usan máx/mín, tooltip muestra Apert./Máx/Mín/Cierre; solo con un único símbolo (con "Todos" cae a línea). `chartType` persistido en localStorage. 2 tests nuevos de parseo OHLC. 156 verdes. Smoke Playwright: velas ON → 21 velas en 1M, línea transparente, techo/suelo 1725/1060; OFF → línea restaurada; persistencia OK; 0 errores.

#### HV-040 Selector de margen del eje Y (compresión/extensión) ✅
- **Estado**: ✅ Completado · **Período**: 2026-07-06 · **Resultado**: ✅ Cumplido · **Tipo**: Evolutivo
- **Spec**: `_duran/specs/HV-040.md`
- **Resumen**: Sustituye el margen fijo de HV-039 (±20 absoluto) por un **selector** `#yMarginSelect` en el gráfico: **Ajustado** (0 %, máxima extensión) · ±1/±2/±5/±10/±20 % (más compresión). El margen es **relativo al precio medio** del rango (`margin = ((techo+suelo)/2)×pct`), así vale para cualquier instrumento (1400 o 3). `Ajustado` deja la línea pegada a los bordes (con epsilon mínimo si los datos son planos). Preferencia persistida en `localStorage` (`chartYMargin`, default 2 %); el cambio recalcula el eje **sin refetch** (`lastRenderedSeries`). Las líneas de techo/suelo se mantienen. Solo JS/cshtml, sin migración. 154 verdes. Smoke Playwright: 1M (techo 1700/suelo 1120) → Ajustado 1120–1700, ±5 % 1049,5–1770,5, ±20 % 838–1982, ±2 % 1091,8–1728,2; persistencia OK; 0 errores.

#### HV-039 Líneas de techo/suelo + margen en el eje Y del gráfico de precios ✅
- **Estado**: ✅ Completado · **Período**: 2026-07-06 · **Resultado**: ✅ Cumplido · **Tipo**: Evolutivo
- **Spec**: `_duran/specs/HV-039.md`
- **Resumen**: El eje Y auto-escalaba pegado a los datos (p.ej. 1350–1380) sin referencia del máximo/mínimo. Ahora `renderChart` calcula el **techo** (máx) y **suelo** (mín) del rango y fija el eje con `min = suelo − 20` / `max = techo + 20` (`PRICE_Y_MARGIN = 20`), como margen visual. Nuevo plugin `highLowLinesPlugin` dibuja dos **líneas discontinuas** horizontales en techo y suelo, etiquetadas ("Techo …" / "Suelo …"), leyendo `chart.$hiLo`. Color según tema; el gráfico de valor de cuenta no cambia. Solo JS, sin migración. 154 verdes. Smoke Playwright: 1M techo 1700/suelo 1120 → eje 1100–1720; 5D 1190/1570 → 1170/1590; 0 errores; screenshot con "Suelo 1190,00" y margen aplicado.

#### HV-038 Gráfico de precios sin huecos de fin de semana/festivos (eje de categorías) ✅
- **Estado**: ✅ Completado · **Período**: 2026-07-06 · **Resultado**: ✅ Cumplido · **Tipo**: Bugfix/mejora
- **Spec**: `_duran/specs/HV-038.md`
- **Resumen**: En 5D/1M/… el eje de **tiempo** dejaba los sábados/domingos, festivos y noches como espacio en blanco y unía los puntos con una **diagonal recta** engañosa. `renderChart` pasa a eje de **categorías** (índice por instante de cotización): solo aparecen los instantes con datos → los findes/festivos **desaparecen**. Rangos diarios/semanales agrupan por **día UTC** (casan bolsas distintas); cada serie se alinea por índice y deja `null` (con `spanGaps:false`) donde no cotiza, sin inventar recta. Etiquetas por span (`labelFormatterFor`) y tooltip con fecha/hora completa (`chart.$labelsFull`); el `currentValuePlugin` se adapta a datos numéricos con `null`. Volumen verde/rojo y variación del rango (HV-037) intactos; el gráfico de valor de cuenta no cambia. Solo JS, sin migración. 154 verdes. Smoke Playwright: eje `category`, `HY9H.F` 5D 124 pts saltando el finde (`03 jul`→`06 jul`), 1M 21 días hábiles, 1D 59 intradía, 0 errores.

#### HV-037 Variación acumulada del rango + eliminar "En vivo" ✅
- **Estado**: ✅ Completado · **Período**: 2026-07-06 · **Resultado**: ✅ Cumplido · **Tipo**: Evolutivo
- **Spec**: `_duran/specs/HV-037.md`
- **Resumen**: El gráfico de precios pierde el modo **"En vivo"** (sin sentido desde HV-027) y gana la **variación acumulada del rango**. Se quita el botón `data-range="LIVE"` y los controles solo-vivo (`#refreshSelect` "Gráfico cada", `#historySelect` "Histórico: N puntos", `#tickCountdown`); el código JS de LIVE queda inerte. Nuevo `#chartDelta` en la cabecera: `updateChartDelta(series)` toma primer→último cierre de la serie del símbolo y muestra `‹rango›: Δimporte (Δ%)` con el **importe en la divisa del instrumento** (`money()`) y el **% acumulado** (`pctSigned()`), coloreado verde/rojo. `symbolCurrency` se alimenta de posiciones y de `/api/instruments/tracked` (arranque + cada 60 s). Los flujos que forzaban LIVE (añadir/quitar símbolo, alta de posición) ahora recargan el histórico del rango activo. Yahoo sigue como proveedor activo. Solo JS/cshtml, sin migración. 154 verdes. Smoke Playwright: sin "En vivo"; `HY9H.F` → `1M +210,00 € (+18,03 %)` verde, `5D −145,00 € (−9,54 %)` rojo, `1D 0,00 € (+0,00 %)` plano, 0 errores de consola.

#### HV-036 Mensaje de error claro del proveedor (Twelve Data) + log sin stack ✅
- **Estado**: ✅ Completado · **Período**: 2026-07-06 · **Resultado**: ✅ Cumplido · **Tipo**: Mejora
- **Spec**: `_duran/specs/HV-036.md`
- **Resumen**: Con Twelve Data (plan free) el feed logueaba `HttpRequestException 404` con stack cada ciclo. `TwelveDataProvider` ahora parsea el body de error (JSON `{status,message}` también en 4xx) y lanza el **mensaje de TD** ("available starting with the Grow plan"); el generador loguea solo el mensaje (una línea, sin stack). 1 test (404 con cuerpo → mensaje). 154 verdes. **Diagnóstico**: el plan free de Twelve Data es solo US; `HY9H`/`000660`/`NUCL.SW` requieren plan de pago → en free usar símbolos US o seguir con Yahoo.

#### HV-035 Fix: el histórico no rompe con símbolos no válidos (404) ✅
- **Estado**: ✅ Completado · **Período**: 2026-07-06 · **Resultado**: ✅ Cumplido · **Tipo**: Bugfix
- **Spec**: `_duran/specs/HV-035.md`
- **Resumen**: Al cambiar a Twelve Data el programa paraba con `HttpRequestException 404`: el frontend recargaba el histórico del símbolo estilo Yahoo (`^GSPC`/`HY9H.F`), que no existe en TD → 404 → `EnsureSuccessStatusCode` lanzaba y `History` solo capturaba `ArgumentException`. Fix: `TwelveDataHistoryProvider`/`YahooHistoryProvider` degradan a **serie vacía** en no-éxito/error (log, no lanzan); `DashboardController.History` con `catch` amplio → serie vacía. 2 tests (404→vacío, error→vacío). 153 verdes. Smoke real: con TD, `^GSPC`/`HY9H.F` → 200 vacío; `AAPL` → 100 puntos; sin crash.

#### HV-034 Buscador de instrumentos vía Twelve Data (selector) ✅
- **Estado**: ✅ Completado · **Período**: 2026-07-06 · **Resultado**: ✅ Cumplido
- **Spec**: `_duran/specs/HV-034.md`
- **Resumen**: El buscador usa el proveedor activo. `TwelveDataInstrumentSearchProvider` (`/symbol_search`) → símbolos con la convención de TD; `SelectableInstrumentSearchProvider` delega por `IMarketProviderState`. Resuelve la limitación de HV-032 (símbolos Yahoo no resuelven en TD). 3 tests. 152 verdes. Smoke real: con TD, `q=apple` → AAPL/APC…; y hallazgo: SK hynix Frankfurt en TD = `HY9H`. Key en user-secrets (no en repo).

#### HV-033 Selector de proveedor de datos en la interfaz (cambio en caliente) ✅
- **Estado**: ✅ Completado · **Período**: 2026-07-06 · **Resultado**: ✅ Cumplido
- **Spec**: `_duran/specs/HV-033.md`
- **Resumen**: Cambiar Yahoo ↔ Twelve Data **desde la UI en runtime** (antes solo por config al arrancar). `IMarketProviderState`/`MarketProviderState` (conmutable + persistido en `App_Data/active-provider.txt`) + wrappers `SelectableMarketDataProvider`/`SelectableMarketHistoryProvider` que delegan en el activo por llamada. DI registra ambos concretos + wrappers + estado (fuera el selector de arranque). `DashboardService` reporta el activo. `ProviderController` (`GET`/`POST /api/provider`). UI: badge → **selector** en cabecera (aviso "⚠ sin API key"). 3 tests nuevos (`MarketProviderState`) + tests adaptados al nuevo ctor. 149 verdes. Smoke real: switch por API (dashboard refleja el cambio, inválido→400, vuelta a Yahoo) + Playwright del selector. `.gitignore` excluye el fichero de estado.

#### HV-032 Histórico de Twelve Data (/time_series) + mapeo de símbolos ✅
- **Estado**: ✅ Completado · **Período**: 2026-07-06 · **Resultado**: ✅ Cumplido
- **Spec**: `_duran/specs/HV-032.md`
- **Resumen**: Cuando el proveedor activo es Twelve Data, el **histórico** también usa Twelve Data. `TwelveDataHistoryProvider : IMarketHistoryProvider` (`/time_series`): parsea `values[]` (close+volume, datetime UTC), invierte a orden ascendente, guardas de key/error. Mapa rango→(interval,outputsize) incl. YTD por días desde 1-ene. `NormalizeSymbol` (cripto Yahoo→barra TD). DI: `useTwelveData` unifica el selector de feed en vivo **y** de histórico (Yahoo default | TwelveData). Sin migración. 4 tests. 146 verdes. Smoke real: default `/api/history AAPL 1D`=79 pts con volumen; `ProviderType=TwelveData` arranca sin errores de DI. Limitación: símbolos estilo Yahoo (`HY9H.F`,`^GSPC`) pueden no resolver en TD (convención distinta).

#### HV-031 Segundo proveedor de datos — Twelve Data ✅
- **Estado**: ✅ Completado · **Período**: 2026-07-06 · **Resultado**: ✅ Cumplido
- **Spec**: `_duran/specs/HV-031.md`
- **Resumen**: Añade **Twelve Data** como 2º proveedor en vivo (Fase 3), reutilizando `IMarketDataProvider`. `TwelveDataOptions` (`MarketData:TwelveData`: BaseUrl, ApiKey, TimeoutSeconds) + `TwelveDataProvider` (endpoint `/quote` → precio+volumen+divisa; parseo string→decimal; guardas de API key y de `status:"error"`). DI: HttpClient "TwelveData" siempre registrado + **selector** por `MarketData:ProviderType` ("TwelveData" | "YahooFinance" default). Badge de proveedor para Twelve Data. El histórico (barra de rangos) sigue en Yahoo. Sin migración. 5 tests. 142 verdes. Smoke real: default → `YahooFinance`; env `ProviderType=TwelveData` → `TwelveData`, arranca OK. Para usarlo: fijar `ProviderType=TwelveData` + `ApiKey` (twelvedata.com, plan gratuito 8 req/min).

#### HV-030 Barras de volumen coloreadas por dirección (verde/rojo) ✅
- **Estado**: ✅ Completado · **Período**: 2026-07-06 · **Resultado**: ✅ Cumplido
- **Spec**: `_duran/specs/HV-030.md`
- **Resumen**: Refinamiento de HV-029. Las barras de volumen pasan de un color único a **verde** (precio sube en esa barra, alcista) / **rojo** (precio baja, bajista) — convención financiera estándar. `backgroundColor` como array por barra: `up = precio[i] >= precio[i-1]` → `#198754@0.5` / `#dc3545@0.5`; primera barra alcista. Solo JS. Verificado con Playwright: barras coherentes con la línea de precio, sin errores de consola.

#### HV-029 Volumen de negociación en el gráfico de precios ✅
- **Estado**: ✅ Completado · **Período**: 2026-07-06 · **Resultado**: ✅ Cumplido
- **Spec**: `_duran/specs/HV-029.md`
- **Resumen**: Muestra el **volumen** como barras en un eje secundario bajo la línea de precio (patrón financiero). Viene de Yahoo por el mismo endpoint (`indicators.quote[0].volume`). `PricePoint.Volume` (default 0), `YahooHistoryProvider` parsea el array de volumen, `DashboardService` pasa `MarketTick.Volume` a la serie LIVE. Frontend: dataset `type:'bar'` en eje oculto `yVol` (`max=maxVol×4`, ~25% inferior), color de la serie al 16 %, fuera de leyenda, tooltip entero. Sin migración. 2 tests del parseo. 137 verdes. Verificado con Playwright: barras visibles en 1D bajo el precio, sin errores de consola.

#### HV-028 Persistir el rango del gráfico de precios entre sesiones ✅
- **Estado**: ✅ Completado · **Período**: 2026-07-06 · **Resultado**: ✅ Cumplido
- **Spec**: `_duran/specs/HV-028.md`
- **Resumen**: Última preferencia del panel que no se guardaba: el **rango del gráfico de precios** (barra LIVE/1D/5D/…). `initRangeBar` guarda `chartRange` en `localStorage` al hacer clic y lo restaura al iniciar (marca el botón activo y fija `chartMode`); sin preferencia → default 1D (HV-027). Validación por inclusión en los `data-range` existentes. Verificado con Playwright: 1D → 5D → reload → sigue en 5D. Con esto **todas** las preferencias sobreviven entre sesiones: `theme`, `chartSymbol`, `chartRefreshMs`, `historyPoints`, `accountRange`, `chartRange`.

#### HV-027 El gráfico de precios arranca en el rango diario (1D) ✅
- **Estado**: ✅ Completado · **Período**: 2026-07-06 · **Resultado**: ✅ Cumplido
- **Spec**: `_duran/specs/HV-027.md`
- **Resumen**: La vista "En vivo" (ticks locales dispersos) dejaba de tener sentido como estado inicial. Ahora el gráfico de precios **arranca en 1D** (intradía real de Yahoo): `chartMode` inicial `'1D'`, botón 1D activo por defecto, y en el arranque `fetchAndRender(true).then(loadHistory(chartMode))` dibuja el histórico tras cargar los símbolos. "En vivo" se conserva como opción. Solo JS/cshtml. Verificado con Playwright: al cargar muestra el intradía 1D (HY9H.F ~29 puntos, "Histórico Yahoo · 1D"), sin errores de consola.

#### HV-026 Selector de rango temporal en el gráfico del valor de cuenta ✅
- **Estado**: ✅ Completado · **Período**: 2026-07-06 · **Resultado**: ✅ Cumplido
- **Spec**: `_duran/specs/HV-026.md`
- **Resumen**: Cierra la mejora opcional de HV-025. Sustituye "Puntos: Últimos N" por **"Rango: 1 día / 1 semana / 1 mes / Todo"**. `filterAccountByRange` filtra los snapshots a la ventana **anclada al último snapshot** (no a `Date.now()`, para que el zoom funcione aunque la app haya estado apagada). `fetchAccountHistory` cachea los puntos y el cambio de rango re-filtra sin refetch; rango persistido en localStorage. Solo JS/cshtml. Verificado con Playwright: rango "1 día" muestra el tramo reciente legible (eje en minutos, curva 28.306→26.711→~32.800), sin errores de consola.

#### HV-025 Cortar huecos entre sesiones en el gráfico del valor de cuenta ✅
- **Estado**: ✅ Completado · **Período**: 2026-07-06 · **Resultado**: ✅ Cumplido
- **Spec**: `_duran/specs/HV-025.md`
- **Resumen**: Resuelve el hallazgo #2 de la revisión visual. El "pico" del gráfico de cuenta no era un outlier: la inspección de `/api/account/history` mostró datos legítimos (8 snapshots del 29-jun ~28.306 € + 4 del 06-jul con el movimiento real en ~20 min). El problema era el mismo que HV-024: la línea cruzaba un hueco de ~6,7 días comprimiendo el tramo reciente. Fix (solo JS): `insertLiveGaps` refactorizado a `insertGaps(points, gapMs)` genérico + `ACCOUNT_GAP_MS` (30 min) aplicado a ambas series del gráfico de cuenta con `spanGaps:false`. **Sin borrar datos** (son legítimos). Verificado con Playwright: desaparece la línea falsa. Residual honesto: el tramo de ~20 min se ve denso sobre un eje de días (mejora opcional: selector de rango temporal).

#### HV-024 Cortar huecos entre sesiones en el gráfico de precios ✅
- **Estado**: ✅ Completado · **Período**: 2026-07-06 · **Resultado**: ✅ Cumplido
- **Spec**: `_duran/specs/HV-024.md`
- **Resumen**: Revisión visual con **Playwright** (Chrome headless). El gráfico "En vivo" unía ticks de sesiones separadas por días con una diagonal recta engañosa. Fix (solo JS): `insertLiveGaps` inserta un punto nulo entre ticks con Δt > `LIVE_GAP_MS` (5 min) y `spanGaps:false` corta la línea sobre el hueco; solo en modo LIVE (el histórico Yahoo no se toca). Verificado con Playwright: desaparece la diagonal falsa, sin errores de consola. Hallazgo abierto (#2): discontinuidad del gráfico de valor de cuenta por snapshots pre-FX (se auto-corrige; purga opcional).

#### HV-023 Refrescar FX en background ✅
- **Estado**: ✅ Completado · **Período**: 2026-06-29 · **Resultado**: ✅ Cumplido
- **Spec**: `_duran/specs/HV-023.md`
- **Resumen**: Saca la llamada HTTP a la fuente FX del hot path del dashboard. `IFxRateProvider.RefreshAsync` (fuerza fetch ignorando TTL) + `FxRefreshService : BackgroundService` que cada `Fx:RefreshSeconds` (300) refresca los tipos de las divisas en uso (watchlist + caja), excluyendo la base y duplicados. El dashboard sigue usando `GetRateAsync` (caché perezosa con fallback), ahora siempre caliente. 3 tests nuevos. 135 verdes. Smoke real: arranque sin errores con el hosted service.

#### HV-022 PnL convertido por fila ✅
- **Estado**: ✅ Completado · **Período**: 2026-06-29 · **Resultado**: ✅ Cumplido
- **Spec**: `_duran/specs/HV-022.md`
- **Resumen**: Cada fila de posiciones muestra, además del PnL en su divisa, el **equivalente en divisa base** cuando difieren. `OpenTradeDto.UnrealizedPnLBase`/`ClosedTradeDto.RealizedPnLBase` calculados en `DashboardService` (PnL × tipo del símbolo); se reordenó el bloque FX (`RateOf`) para construirlo antes de las filas. UI: helper `pnlCell` que añade `≈ <importe base>` en gris solo si divisa ≠ base. 1 test nuevo. 132 verdes. Sin migración.

#### HV-021 Divisa por movimiento de caja ✅
- **Estado**: ✅ Completado · **Período**: 2026-06-29 · **Resultado**: ✅ Cumplido
- **Spec**: `_duran/specs/HV-021.md`
- **Resumen**: Cada movimiento de caja lleva su **divisa** (antes se asumían todos en EUR). `CashMovement.Currency` (default EUR) + migración `AddCashMovementCurrency` (defaultValue "EUR" para filas previas). `CashMovementDto`/`ICashService.AddAsync`/`CashController` aceptan divisa. `DashboardService` convierte `netDeposits = Σ amount·rate(divisa→base)` (invariante preservado). UI: campo **Divisa** en el alta del modal Caja, importe por fila en su divisa y "Aportado neto" **agrupado por divisa**. 2 tests nuevos. 131 verdes. Smoke real: alta 100 USD → netDeposits 30000→30087,67 (×0,8767) y limpieza OK. Completa la deuda que HV-020 dejó (caja asumida en base).

#### HV-020 Conversión FX a divisa base ✅
- **Estado**: ✅ Completado · **Período**: 2026-06-29 · **Resultado**: ✅ Cumplido
- **Spec**: `_duran/specs/HV-020.md`
- **Resumen**: Los **totales de cuenta** dejan de mezclar divisas: se convierten a una **divisa base** (EUR, config `Fx:BaseCurrency`). `IFxRateProvider`/`FxRateProvider` obtiene tipos vía Yahoo `{FROM}{TO}=X` (p.ej. `USDEUR=X`) con caché en memoria por par + TTL (`Fx:CacheMinutes`, 30) y degradación a último valor/1 si falla. `DashboardService` convierte invested/marketValue/realized/unrealized por el tipo de cada símbolo (aportaciones de caja asumidas en base); invariante `accountValue = netDeposits + totalPnL` preservado. `DashboardDto.BaseCurrency` + nota en UI ("Totales convertidos a EUR"). Las filas de posiciones siguen en su divisa nativa (HV-019). 6 tests nuevos. 129 verdes. Smoke real: `USDEUR=X=0,8768`; dashboard `base=EUR`. Cierra la deuda de divisas de HV-019.

#### HV-019 Divisa por instrumento ✅
- **Estado**: ✅ Completado · **Período**: 2026-06-29 · **Resultado**: ✅ Cumplido
- **Spec**: `_duran/specs/HV-019.md`
- **Resumen**: Captura y muestra la **divisa de cotización** de cada instrumento. `TrackedSymbol` gana `Currency` + `SetCurrency` (+ migración `AddTrackedSymbolCurrency`). El proveedor pasa a devolver `MarketQuote(Tick, Currency)` (Yahoo `/v8/chart` ya informa `meta.currency`) y el `MarketTickGeneratorService` **sella** la divisa en cada símbolo seguido cada ciclo (~30 s) → cubre altas por buscador, manuales e importadas. `Currency` propagado a `TrackedSymbolDto`/`OpenTradeDto`/`ClosedTradeDto` (mapa símbolo→divisa en `DashboardService`). UI: columna "Div" (badge) + formateo monetario por fila en su divisa (`money()`). Los **totales de cuenta siguen en €** (mezclan divisas hasta el paso FX — documentado). 3 tests nuevos. 123 verdes. Smoke real: `HY9H.F→EUR`, `^GSPC→USD`.

#### HV-018 Importar histórico de compras (CSV) ✅
- **Estado**: ✅ Completado · **Período**: 2026-06-29 · **Resultado**: ✅ Cumplido
- **Spec**: `_duran/specs/HV-018.md`
- **Resumen**: Alta masiva de posiciones desde CSV (`symbol, entry, qty[, date]`). `IPositionService.ImportCsvAsync` reutiliza `OpenAsync` por fila; parser con cabecera opcional, delimitador autodetectado (`;`/`,`; con `;` admite coma decimal), fechas múltiples (UTC). Las filas con error se reportan por línea y no abortan el resto. `ImportResultDto`/`ImportErrorDto` + endpoint `POST /api/positions/import`. UI: modal "📥 Importar CSV" (subir fichero vía FileReader o pegar texto) con resultados (importadas/fallidas + errores). 4 tests. 120 verdes. Smoke real: endpoint responde con reporte de error correcto (fila inválida, sin escribir en BD).

#### HV-017 Histórico del valor de cuenta ✅
- **Estado**: ✅ Completado · **Período**: 2026-06-29 · **Resultado**: ✅ Cumplido
- **Spec**: `_duran/specs/HV-017.md`
- **Resumen**: Gráfico de **evolución del valor de cuenta** en el tiempo. Reutiliza la entidad `PortfolioSnapshot` (tabla ya existente en InitialCreate, sin uso hasta ahora → **sin migración**). Nuevo `PortfolioSnapshotService : BackgroundService` que toma un snapshot al arrancar (tras 15 s) y cada `IntervalSeconds` (300, config `Snapshot`), reutilizando `IDashboardService` para computar el valor de cuenta. `AccountHistoryPointDto` + `IDashboardService.GetAccountHistoryAsync` (ordena ascendente; deriva `NetDeposits = Capital − TotalPnL` y `ReturnPct`) + endpoint `GET /api/account/history?points=`. UI: tarjeta "📊 Evolución del valor de cuenta" con gráfico Chart.js (valor de cuenta + línea de aportado neto), refresco propio cada 60 s. 5 tests nuevos (2 de `GetAccountHistoryAsync`, 3 del servicio incl. persistencia end-to-end). 116 tests verdes. Smoke real: `/api/account/history` → 1 punto coherente (28.306,65 € / aportado 30.000 / −5,64 %).

#### HV-016 Rentabilidad por posición ✅
- **Estado**: ✅ Completado · **Período**: 2026-06-29 · **Resultado**: ✅ Cumplido
- **Spec**: `_duran/specs/HV-016.md`
- **Resumen**: % de rendimiento por fila en las tablas de posiciones. `OpenTradeDto.ReturnPct` (`(current−entry)/entry×100`) y `ClosedTradeDto.ReturnPct` (`(exit−entry)/entry×100`) calculados en `DashboardService` (0 % si entry=0). UI: nueva columna **"%"** tras PnL en abiertas y cerradas, con signo y color (reusa `pctSigned`/`signClass`). Sin migración ni cambios de dominio/BD. 2 tests (abierta +10 %, cerrada −10 %). 113 tests verdes.

#### HV-015 Rentabilidad porcentual de la cuenta ✅
- **Estado**: ✅ Completado · **Período**: 2026-06-29 · **Resultado**: ✅ Cumplido
- **Spec**: `_duran/specs/HV-015.md`
- **Resumen**: Métrica derivada del rendimiento de la cuenta sobre el aportado neto. `DashboardDto.ReturnPct` + cálculo en `DashboardService` (`ReturnPct = TotalPnL / NetDeposits × 100`, 0 % si `NetDeposits = 0`; equivale a `(AccountValue − NetDeposits) / NetDeposits × 100` por el invariante de HV-014). UI: el subtítulo de la tarjeta **Valor de cuenta** muestra el % con signo y color (verde/rojo) + "sobre aportado", reutilizando `pctSigned`/`setSigned`. Sin migración ni cambios de dominio/BD. 3 tests (ganancia +0,5 %, pérdida −10 %, sin aportaciones → 0 %). 111 tests verdes.

#### HV-014 Caja / efectivo y valor de cuenta ✅
- **Estado**: ✅ Completado · **Período**: 2026-06-16 · **Resultado**: ✅ Cumplido
- **Spec**: `_duran/specs/HV-014.md`
- **Resumen**: Visión de **cuenta completa**. Entidad `CashMovement` (ingreso/retirada) + migración + `CashService` + endpoints `GET/POST/DELETE /api/cash`. `DashboardService` deriva **Efectivo** (`= aportado − invertido + realizado`) y **Valor de cuenta** (`= efectivo + valor de cartera`); invariante `valor de cuenta = aportado + PnL total`. UI: 6 tarjetas (Valor de cuenta · Efectivo · Valor de cartera · PnL total · Posiciones · Winrate) + modal "💰 Caja" (alta Ingreso/Retirada + lista). 5 tests. Smoke real: aporta 10000 € + HY9H.F 1360×5 → efectivo 3200 €, valor de cuenta 10150 €.

#### HV-013 Posiciones reales del usuario (tracker de cartera) ✅
- **Estado**: ✅ Completado · **Período**: 2026-06-16 · **Resultado**: ✅ Cumplido
- **Spec**: `_duran/specs/HV-013.md`
- **Resumen**: Alta manual de posiciones reales para seguimiento (objetivo: dejar TradingView). `IPositionService`/`PositionService` (open/close/delete **por Id**, varias posiciones por símbolo, auto-añade a watchlist) sobre la entidad `Trade`. Endpoints `POST /api/positions`, `POST /api/positions/{id}/close`, `DELETE /api/positions/{id}`. **Modal** "➕ Nueva posición" (símbolo con datalist de la watchlist, precio entrada, cantidad, fecha) + columna **Acciones** (Cerrar/✕) en la tabla. PnL en vivo contra el precio real. 7 tests. Smoke real: HY9H.F 1360×5 → PnL +150 € (actual 1390); cierre a 1400 → +200 €.

#### HV-012 Eliminar simulación — feed 100% real (panel solo-visor) ✅
- **Estado**: ✅ Completado · **Período**: 2026-06-16 · **Resultado**: ✅ Cumplido
- **Spec**: `_duran/specs/HV-012.md`
- **Resumen**: Se retira RandomWalk (borrado `RandomWalkTickGenerator` + test). `IMarketDataProvider`=`YahooFinanceProvider` siempre; intervalo de sondeo 30 s. Estrategia MA Crossover **desactivada** (no se registra `StrategyExecutionService`) → panel solo-visor. `MarketDataOptions`/`appsettings` reducidos (sin `Symbols`/`Volatility`/`Drift`/`Volume`/`Seed`). Smoke real: `provider=YahooFinance`, tick real HY9H.F=1390€. 96 tests verdes.

#### HV-011 Alta dinámica de símbolos (watchlist persistida) ✅
- **Estado**: ✅ Completado · **Período**: 2026-06-16 · **Resultado**: ✅ Cumplido
- **Spec**: `_duran/specs/HV-011.md`
- **Resumen**: Entidad `TrackedSymbol` + migración EF + `DbSet`. `IWatchlistService`/`WatchlistService` (add/remove/list; al quitar borra los `MarketTick` del símbolo). El generador lee la watchlist de BD **cada ciclo**. Endpoints `GET/POST/DELETE /api/instruments/track[ed]`. Seed `HY9H.F` al arrancar. UI: "+ Añadir" en resultados y "✕" junto al selector. 7 tests de `WatchlistService`.

#### HV-010 Buscador de instrumentos (descripción / ISIN / ticker) ✅
- **Estado**: ✅ Completado · **Período**: 2026-06-16 · **Resultado**: ✅ Cumplido
- **Spec**: `_duran/specs/HV-010.md`
- **Resumen**: `IInstrumentSearchProvider` + `YahooInstrumentSearchProvider` (`/v1/finance/search`) → `GET /api/instruments/search?q=`. Busca por nombre, ISIN y ticker; **WKN no soportado por Yahoo** (fuera de alcance). UI: tarjeta de búsqueda + tabla de resultados con botón "+ Añadir". 8 tests del provider.

#### HV-009 Histórico real de Yahoo en la barra de rangos (Fase 2) ✅
- **Estado**: ✅ Completado · **Período**: 2026-06-15 · **Resultado**: ✅ Cumplido
- **Spec**: `_duran/specs/HV-009.md`
- **Resumen**: `IMarketHistoryProvider` + `YahooHistoryProvider` (`/v8/chart` con `range`/`interval`, normaliza `BTCUSD→BTC-USD`, parsea arrays). Endpoint `GET /api/history?symbol&range`. HttpClient Yahoo registrado siempre. Frontend: barra `1D…5A` carga OHLC reales (modo histórico), "En vivo" vuelve a la simulación, eje X con unidad temporal adaptativa. Smoke real OK (AAPL 1M=22pts, BTCUSD 5A=262pts). 89 tests verdes.

#### HV-008 Fix YahooFinanceProvider → endpoint /v8/chart (Fase 2) ✅
- **Estado**: ✅ Completado
- **Período**: 2026-06-15 → 2026-06-15
- **Duración**: <1 día (sesión única)
- **Resultado**: ✅ Cumplido
- **Tipo**: Bugfix
- **Spec**: `_duran/specs/HV-008.md`
- **Resumen**: El smoke test real (pendiente desde HV-007) reveló que `/v7/finance/quote` devuelve **401** (Yahoo exige cookie+crumb). Migrado `YahooFinanceProvider` a `GET /v8/finance/chart/{symbol}` (precio+volumen sin auth, parseo de `meta`) + User-Agent de navegador. 11 tests reescritos al nuevo shape. **Smoke test real ✅**: AAPL=291,13 / GOOG=358,16 reales; `BTCUSD`→404 capturado per-símbolo (Yahoo usa `BTC-USD`). **89 tests verdes**.

#### HV-007 Provider real Yahoo Finance + abstracción intercambiable (Fase 2) ✅
- **Estado**: ✅ Completado
- **Período**: 2026-05-26 → 2026-05-26
- **Duración**: <1 día (sesión única)
- **Resultado**: ✅ Cumplido
- **Spec**: `_duran/specs/HV-007.md`
- **Resumen**: `IMarketDataProvider` refactorizado a async. `RandomWalkTickGenerator` adaptado (Task.FromResult). `YahooFinanceProvider` nuevo con HttpClientFactory + parsing JSON + validación defensiva. DI selector switch por `MarketData:ProviderType`. Helpers de test reutilizables. 11 tests YahooFinance + 8 RandomWalk migrados a async. **89 tests verdes total. Patrón listo para AlphaVantage/Binance: 1 archivo + 1 case.**

#### HV-006 Dashboard con métricas y gráfico Chart.js ✅
- **Estado**: ✅ Completado
- **Período**: 2026-05-26 → 2026-05-26
- **Duración**: <1 día (sesión única)
- **Resultado**: ✅ Cumplido
- **Spec**: `_duran/specs/HV-006.md`
- **Resumen**: `DashboardService` con cálculo on-the-fly (in-memory por bug SQLite+decimal-TEXT) + DashboardController + Vista Razor + Bootstrap 5 + Chart.js + polling 3s. 7 tests + smoke test runtime ✅: `/api/dashboard/data` devuelve JSON con PnL no realizado calculado en vivo. **78 tests verdes total. MVP completo.**

#### HV-005 Estrategia MA Crossover automática ✅
- **Estado**: ✅ Completado
- **Período**: 2026-05-26 → 2026-05-26
- **Duración**: <1 día (sesión única)
- **Resultado**: ✅ Cumplido
- **Spec**: `_duran/specs/HV-005.md`
- **Resumen**: `ChannelTickBus` pub-sub in-process + `MovingAverageCrossoverStrategy` con rolling windows + `IOrderService` (1 posición/símbolo) + `StrategyExecutionService` BackgroundService. Refactor del generator para publicar al bus tras persistir. 21 tests nuevos. Smoke test: 1 Trade BUY abierto en 95s. **71 tests verdes en total**.

#### HV-004 BackgroundService generador de ticks de mercado ✅
- **Estado**: ✅ Completado
- **Período**: 2026-05-26 → 2026-05-26
- **Duración**: <1 día (sesión única)
- **Resultado**: ✅ Cumplido
- **Spec**: `_duran/specs/HV-004.md`
- **Resumen**: `RandomWalkTickGenerator` (Box-Muller, seedable, thread-safe) + `MarketTickGeneratorService : BackgroundService` (PeriodicTimer + ScopeFactory). 3 símbolos configurables. 8 tests verdes + smoke test runtime: 12 ticks persistidos en BD en 12s con precios coherentes.

#### HV-003 Persistencia SQLite + EF Core Migrations ✅
- **Estado**: ✅ Completado
- **Período**: 2026-05-26 → 2026-05-26
- **Duración**: <1 día (sesión única)
- **Resultado**: ✅ Cumplido
- **Spec**: `_duran/specs/HV-003.md`
- **Resumen**: EF Core 10.0.8 + SQLite + 3 entity configurations (decimal→TEXT), DI extension, auto-migrate al arrancar, 6 tests integración con SQLite `:memory:`, smoke test runtime con `App_Data/trading.db` creado. Total tests: 42 verdes.

#### HV-002 Modelo de dominio (Trade, MarketTick, PortfolioSnapshot, TradeStatus) ✅
- **Estado**: ✅ Completado
- **Período**: 2026-05-26 → 2026-05-26
- **Duración**: <1 día (sesión única)
- **Resultado**: ✅ Cumplido
- **Spec**: `_duran/specs/HV-002.md`
- **Resumen**: 4 tipos en Domain (3 entidades + 1 enum) con factories, invariantes y propiedades calculadas (`RealizedPnL`, `TotalPnL`). 36 tests verdes. Domain sin NuGet.

#### HV-001 Scaffold inicial de la solución (Clean Architecture) ✅
- **Estado**: ✅ Completado
- **Período**: 2026-05-26 → 2026-05-26
- **Duración**: <1 día (sesión única)
- **Resultado**: ✅ Cumplido
- **Spec**: `_duran/specs/HV-001.md`
- **Resumen**: `.slnx` + 5 proyectos Clean Architecture (Domain/Application/Infrastructure/Web/Tests) con referencias correctas, `TreatWarningsAsErrors=true`, `.gitignore` raíz, `App_Data/.gitkeep`, Solution Folders STIC.IA. `dotnet build` y `dotnet test` verdes.

---

## Indice de Modulos

| Modulo | Descripcion | Estado | Criticidad |
|--------|-------------|--------|------------|
| Dashboard | Vista principal con balance, pnl, operaciones y grafico de evolucion | Planificado (MVP) | Alta |
| Simulacion de Mercado | Generacion de ticks fake (symbol/price/volume/timestamp) | Planificado (MVP) | Alta |
| Estrategia MA Crossover | Compra/venta automatica segun cruce de medias moviles corta/larga | Planificado (MVP) | Alta |
| Simulador de Ordenes | Buy / Sell / Close Position con persistencia | Planificado (MVP) | Alta |
| Portfolio | Tracking de capital, pnl, drawdown, winrate | Planificado (MVP) | Alta |
| Metricas | Operaciones ganadas/perdidas, profit factor, drawdown, pnl acumulado | Planificado (MVP) | Media |

---

## Detalle de Funcionalidades

### Modulo: Dashboard

#### DASH-001: Pagina principal con metricas en tiempo real

**Descripcion**: Vista de aterrizaje que muestra el estado actual del simulador en una pantalla. Combina datos de Portfolio + Trades + grafico.

**Usuario objetivo**: Yo (uso personal).

**Componentes UI**:
- Card balance virtual (capital actual + variacion %)
- Card pnl total
- Tabla operaciones abiertas
- Tabla operaciones cerradas (ultimas N)
- Grafico Chart.js de evolucion del portfolio en el tiempo

**Archivos principales (a crear)**:
```
Web/Controllers/DashboardController.cs
Web/Views/Dashboard/Index.cshtml
Web/ViewModels/DashboardViewModel.cs
Application/Services/IDashboardService.cs
Application/Services/DashboardService.cs
```

**Dependencias**: Portfolio, Trades, MarketTicks ya persistidos.

---

### Modulo: Simulacion de Mercado

#### MKT-001: Generador de ticks aleatorios controlados

**Descripcion**: BackgroundService que cada N milisegundos emite un tick (symbol, price, volume, timestamp). Precio sigue un random walk con parametros configurables (volatilidad, drift).

**Reglas de negocio**:
- Inicialmente sin conexion a APIs reales
- Datos aleatorios controlados (no caos puro, random walk realista)
- Frecuencia configurable en appsettings.json
- Lista de simbolos configurable en appsettings.json

**Roadmap (Fase 2)**: Sustituir generador fake por integraciones reales (Yahoo Finance, Binance, AlphaVantage).

**Archivos principales**:
```
Infrastructure/MarketData/MarketTickGeneratorService.cs (BackgroundService)
Domain/Entities/MarketTick.cs
Application/Interfaces/IMarketDataProvider.cs
```

---

### Modulo: Estrategia MA Crossover

#### STRAT-001: Moving Average Crossover

**Descripcion**: Estrategia automatica que abre/cierra trades segun cruce de medias moviles.

**Reglas de negocio**:
- COMPRAR cuando MA corta > MA larga (cruce alcista)
- VENDER cuando MA corta < MA larga (cruce bajista)
- Ventanas (corta/larga) configurables en appsettings.json
- Ejecucion en BackgroundService que escucha nuevos ticks

**Archivos principales**:
```
Application/Strategies/IStrategy.cs
Application/Strategies/MovingAverageCrossoverStrategy.cs
Infrastructure/Background/StrategyExecutionService.cs
```

**Roadmap (Fase 3)**: Mas estrategias + IA predictiva con ML.NET/ONNX.

---

### Modulo: Simulador de Ordenes

#### ORD-001: Operaciones Buy / Sell / Close Position

**Descripcion**: Logica de apertura, cierre y consulta de trades. Toda operacion se persiste en SQLite.

**Reglas de negocio**:
- Buy crea Trade con Status=Open
- Sell/Close completa el Trade con ExitPrice y ClosedAt, calculando pnl
- Quantity y EntryPrice fijados al abrir; ExitPrice solo se asigna al cerrar
- Validar que no se cierra un trade ya cerrado

**Archivos principales**:
```
Application/Services/IOrderService.cs
Application/Services/OrderService.cs
Domain/Entities/Trade.cs
Domain/Enums/TradeStatus.cs (Open, Closed)
Infrastructure/Repositories/TradeRepository.cs
```

---

### Modulo: Portfolio

#### PORT-001: Estado y evolucion del portfolio

**Descripcion**: Calculo en tiempo real del estado financiero del simulador.

**Metricas**:
- Capital inicial (configuracion)
- Capital actual (capital inicial + pnl realizado + valoracion abiertos)
- PnL total y por trade
- Drawdown (caida maxima desde pico)
- Winrate (% trades ganadores)
- Profit Factor (suma ganancias / suma perdidas)

**Archivos principales**:
```
Application/Services/IPortfolioService.cs
Application/Services/PortfolioService.cs
Domain/Entities/PortfolioSnapshot.cs (opcional, para grafico evolucion)
Infrastructure/Background/PortfolioUpdaterService.cs
```

---

## Funcionalidades Planificadas (Backlog Roadmap)

| Fase | Codigo | Nombre | Descripcion |
|------|--------|--------|-------------|
| 2 | API-001 | Conexion Yahoo Finance | Sustituir generador fake por feed real |
| 2 | API-002 | Conexion Binance | Datos cripto en tiempo real |
| 2 | API-003 | Conexion AlphaVantage | Datos historicos para backtesting riguroso |
| 3 | ML-001 | Modelo predictivo ML.NET | Senales con clasificador entrenado |
| 3 | ML-002 | ONNX runtime | Cargar modelos entrenados externos |
| 4 | LIVE-001 | Paper trading real | Ejecucion automatica en cuenta demo |

---

## Restricciones (no implementar inicialmente)

- Autenticacion / login
- Microservicios
- Docker / Kubernetes
- Mensajeria distribuida (RabbitMQ, Kafka)
- Trading con dinero real

---

## Glosario rapido (ver tambien CLAUDE.md)

- **Trade**: operacion completa (apertura + cierre)
- **MarketTick**: snapshot de precio en un instante (symbol/price/volume/timestamp)
- **Portfolio**: estado financiero acumulado del simulador
- **MA Crossover**: estrategia de cruce de medias moviles
- **PnL**: Profit and Loss
- **Drawdown**: caida desde pico maximo de capital
- **Winrate**: porcentaje de trades cerrados con ganancia
- **Backtesting**: ejecutar estrategia sobre datos historicos
- **Profit Factor**: suma de ganancias / suma de perdidas
- **Sharpe Ratio**: rentabilidad ajustada por volatilidad
- **Position**: trade actualmente abierto
- **Slippage**: diferencia entre precio esperado y precio real ejecutado
- **Spread**: diferencia entre bid y ask

---

**Ultima actualizacion**: 2026-05-26
**Actualizado por**: stic.claude3 (via /onboarding)
