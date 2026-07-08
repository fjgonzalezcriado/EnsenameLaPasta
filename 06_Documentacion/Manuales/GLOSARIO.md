# Glosario de Términos — AI Trading Simulator

> Definiciones de los términos de negocio y técnicos usados en la aplicación y en el
> [Manual de Usuario](MANUAL_USUARIO.md). Consolida el glosario de dominio del proyecto.

| Campo | Valor |
|-------|-------|
| **Versión** | 1.0 (2026-07-07) |
| **Ámbito** | Tracker de cartera y precios reales (.NET 10) |

---

## 1. Cartera y operativa

| Término | Definición |
|---|---|
| **Trade** | Operación completa (apertura + cierre). Tiene precio de entrada (`EntryPrice`), precio de salida (`ExitPrice`, al cerrar), cantidad (`Quantity`) y estado (`Open`/`Closed`). |
| **Posición (Position)** | Trade **actualmente abierto** (aún no vendido). Su valor se revaloriza con el precio real. |
| **Watchlist** | Lista de instrumentos que sigues. En el modelo es la entidad `TrackedSymbol` (símbolo + divisa). El feed pide el precio de cada símbolo de la watchlist en cada ciclo. |
| **Instrumento** | Activo financiero negociable: acción, índice, ETF, cripto, etc. Se identifica por su **símbolo/ticker** (p. ej. `AAPL`, `^GSPC`, `HY9H.F`). |
| **Efectivo (Cash)** | Dinero disponible en la cuenta: aportado − invertido + realizado − comisiones. |
| **Valor de cuenta (Account Value)** | Efectivo + valor de mercado de la cartera. Se cumple el invariante `valor de cuenta = aportado neto + PnL total`. |
| **Aportado neto (Net Deposits)** | Suma de aportaciones menos retiradas de caja, convertida a divisa base. |
| **Movimiento de caja (CashMovement)** | Aportación o retirada de efectivo, con su divisa. |
| **Dividendo (Dividend)** | Pago periódico que reparte un instrumento a sus tenedores. Se registra (símbolo, importe, divisa, fecha) y suma al PnL realizado. |
| **Comisión (Commission)** | Coste que cobra el bróker por operar. Por defecto **Trade Republic: 1 € fijo por orden** (2 € por operación completa: compra + venta). |
| **Bróker** | Intermediario a través del cual compras/vendes (aquí, referencia a Trade Republic para la tarifa de comisión). |

## 2. Rentabilidad y métricas

| Término | Definición |
|---|---|
| **PnL** | *Profit and Loss*: ganancia o pérdida, en importe o porcentaje. |
| **PnL realizado** | Ganancia/pérdida de trades **cerrados** (incluye comisiones y dividendos). |
| **PnL no realizado** | Ganancia/pérdida «sobre el papel» de las posiciones **abiertas** frente al precio actual. |
| **PnL total** | Realizado + no realizado. |
| **Rentabilidad % (Return %)** | Rendimiento sobre lo aportado: `PnL total / aportado neto × 100`. |
| **Índice de retorno** | Serie `1 + PnL/aportado` usada para calcular métricas de forma **neutral a aportaciones/retiradas** (un ingreso de caja no cuenta como rentabilidad). |
| **Winrate** | % de trades cerrados con ganancia (p. ej. 55 % = 55 de cada 100 trades ganan). |
| **Drawdown** | Caída desde el pico máximo histórico. **Max Drawdown**: la peor caída pico→valle. |
| **Sharpe Ratio** | Rentabilidad ajustada por volatilidad (anualizada). Mayor es mejor relación rentabilidad/riesgo. Necesita suficientes datos diarios para ser fiable. |
| **Volatilidad** | Dispersión de los retornos (anualizada, √252). A mayor volatilidad, mayor riesgo. |
| **Profit Factor** | Suma de ganancias / suma de pérdidas de los trades cerrados. > 1 es rentable (∞ si solo hay ganancias). |
| **Backtesting** | Evaluar una estrategia sobre datos históricos antes de aplicarla. |

## 3. Mercado y precios

| Término | Definición |
|---|---|
| **MarketTick** | Snapshot del precio de un símbolo en un instante: símbolo, precio, volumen, marca de tiempo. |
| **PortfolioSnapshot** | Foto periódica del estado de la cuenta (capital, PnL) usada para el gráfico de evolución. |
| **OHLC** | *Open, High, Low, Close*: apertura, máximo, mínimo y cierre de un periodo. Base de las **velas japonesas**. |
| **Vela japonesa (Candle)** | Representación gráfica de un periodo con su OHLC: cuerpo (apertura↔cierre) y mecha (máx↔mín); verde si sube, roja si baja. |
| **Volumen** | Cantidad negociada en un periodo. Se dibuja como barras bajo el precio. |
| **Volatilidad (de mercado)** | Magnitud de las oscilaciones de precio (ver también la métrica de cartera). |
| **Spread** | Diferencia entre el precio de compra (bid) y de venta (ask). |
| **Slippage** | Diferencia entre el precio esperado y el precio realmente ejecutado. |
| **Divisa base** | Moneda en la que se consolidan los totales de la cuenta (EUR por defecto, `Fx:BaseCurrency`). |
| **FX (tipo de cambio)** | Conversión entre divisas. La app obtiene los tipos de un proveedor y convierte cada importe a la divisa base. |
| **YTD** | *Year To Date*: desde el 1 de enero del año en curso hasta hoy. |
| **Rango** | Ventana temporal del gráfico: `1D`, `5D`, `1M`, `3M`, `6M`, `YTD`, `1A`, `3A`, `5A`. |
| **Techo / Suelo** | Máximo y mínimo del precio en el rango mostrado (líneas de referencia del gráfico). |

## 4. Estrategia e indicadores técnicos

| Término | Definición |
|---|---|
| **MA Crossover** | Estrategia de cruce de medias móviles: comprar cuando la media corta cruza por encima de la larga, vender en el cruce contrario. *(Desactivada en la app: código conservado, sin operar.)* |
| **SMA** | *Simple Moving Average*: media móvil simple de los últimos N precios. |
| **EMA** | *Exponential Moving Average*: media móvil que pondera más los datos recientes. |
| **Momentum** | Variación del precio en los últimos N periodos (fuerza de la tendencia). |
| **RSI** | *Relative Strength Index* (0–100): mide si un activo está sobrecomprado (>70) o sobrevendido (<30). |
| **MACD** | *Moving Average Convergence Divergence*: diferencia entre dos EMAs (12 y 26) y su señal (EMA 9); su histograma indica impulso. |
| **Bollinger %B** | Posición del precio dentro de las bandas de Bollinger (0 = banda inferior, 1 = superior). |
| **Estocástico %K** | Oscilador (0–1) que sitúa el cierre respecto al rango máximo/mínimo de N periodos. |

## 5. Machine Learning (señales orientativas)

| Término | Definición |
|---|---|
| **ML.NET** | Librería de machine learning de .NET usada para las señales. |
| **SSA** | *Singular Spectrum Analysis*: técnica de series temporales para **pronosticar** el precio a N periodos con banda de confianza. |
| **Clasificación sube/baja** | Modelo binario que estima la probabilidad de que el precio **suba** el próximo periodo → señal Comprar/Vender/Mantener. |
| **SDCA** | *Stochastic Dual Coordinate Ascent*: entrenador de regresión logística (uno de los dos modelos de clasificación). |
| **FastTree** | Modelo de árboles con *boosting* (el segundo candidato); se elige el de mayor **AUC** en validación. |
| **Feature** | Variable de entrada del modelo (retorno, momentum, RSI, MACD, volumen relativo…). |
| **Accuracy** | % de aciertos del modelo en el conjunto de validación (hold-out). |
| **AUC** | *Area Under the ROC Curve* (0,5 = azar, 1 = perfecto): calidad de la clasificación. |
| **Hold-out / split cronológico** | Se entrena con el 80 % más antiguo y se evalúa con el 20 % más reciente (evaluación honesta, sin mirar el futuro). |

## 6. Proveedores y técnico

| Término | Definición |
|---|---|
| **Proveedor de datos** | Fuente de precios/histórico/búsqueda. Conmutable en caliente: **Yahoo Finance** (sin API key), **Twelve Data** y **Alpha Vantage** (requieren API key y tienen límites gratuitos). |
| **API key** | Credencial de acceso a un proveedor. Se guarda en *user-secrets*, **nunca** en el repositorio. |
| **Feed** | Proceso en segundo plano que pide el precio de cada símbolo de la watchlist cada ~30 s. |
| **Snapshot (de cuenta)** | Punto guardado periódicamente para dibujar la evolución del valor de cuenta. |
| **Watchlist persistida** | La watchlist se guarda en la base de datos (`TrackedSymbol`), sobrevive a reinicios. |
| **ISIN** | *International Securities Identification Number*: código internacional de 12 caracteres que identifica un valor (p. ej. `US0378331005`). |
| **WKN** | Código alemán de identificación de valores. **No soportado** por el buscador (Yahoo no lo indexa). |
| **SQLite** | Base de datos local en fichero (`App_Data/trading.db`) donde se guardan watchlist, posiciones, caja, dividendos y snapshots. |
| **Modo Visor** | Estado de solo lectura del panel: oculta los controles que modifican datos. |

---

*Glosario del proyecto AI Trading Simulator. Fuente de dominio: `CLAUDE.md` y `_duran/`.*
