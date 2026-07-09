# Manual de Usuario — AI Trading Simulator

> Tracker de cartera y precios reales (ASP.NET Core MVC · .NET 10). Panel web para seguir
> instrumentos financieros, registrar tus posiciones reales y ver métricas de rendimiento.

---

## Información del Documento

| Campo | Valor |
|-------|-------|
| **Aplicación** | AI Trading Simulator |
| **Versión de la app** | 1.39.0 |
| **Tipo** | Aplicación web local (uso personal) |
| **Idioma** | Español (es-ES), formato monetario europeo |
| **Fecha** | 2026-07-07 |
| **Glosario** | Ver [GLOSARIO.md](GLOSARIO.md) para los términos marcados con *(→ glosario)* |

## Control de Versiones

| Versión doc. | Fecha | Cambios |
|---|---|---|
| 1.0 | 2026-07-07 | Versión inicial del manual (cubre HV-001…HV-050). |

---

## 1. Introducción

### 1.1 ¿Qué es AI Trading Simulator?

Es una aplicación web **personal** para **seguir precios reales** de instrumentos financieros
(acciones, índices, cripto, ETFs…) y **llevar el control de tu cartera**. Nació como simulador de
trading algorítmico y evolucionó a un **tracker de precios reales**:

- Busca instrumentos y crea tu **watchlist** *(→ glosario)*.
- Registra tus **posiciones** reales (compra/venta) y ve su **PnL** *(→ glosario)* en vivo.
- Controla la **caja** (aportaciones/retiradas) y los **dividendos**.
- Consulta **gráficos** de precio (línea o velas) y de **valor de cuenta**.
- Obtén **métricas** de rendimiento (rentabilidad, drawdown, Sharpe, profit factor).
- Consulta **señales ML** orientativas (pronóstico y clasificación sube/baja).

> **Importante**: NO ejecuta operaciones reales ni automáticas. Es un **panel de seguimiento**
> (visor). No es asesoramiento financiero.

### 1.2 ¿A quién va dirigida?

A **un único usuario** (el propietario), que hace de administrador y usuario. No hay cuentas,
login ni multiusuario.

### 1.3 Requisitos previos

- **.NET 10 SDK** instalado.
- **Conexión a Internet** (los precios y los tipos de cambio se obtienen de proveedores externos como
  Yahoo Finance).
- Un navegador moderno (Chrome, Edge, Firefox).
- La base de datos (SQLite, `App_Data/trading.db`) se crea sola al arrancar.

---

## 2. Acceso a la Aplicación

### 2.1 URL de acceso

La app corre en local. Arráncala desde la carpeta del proyecto web:

```bash
cd 03_Desarrollo/EnsenameLaPasta.Web
dotnet run
```

Abre en el navegador la URL que indique la consola (p. ej. **`https://localhost:5001`**).

### 2.2 Inicio de sesión

**No hay inicio de sesión.** Al abrir la URL entras directamente al panel.

### 2.3 Primer acceso

- Al arrancar por primera vez se crea la base de datos y se **siembra un símbolo** en la watchlist
  (`HY9H.F`) para que el panel no esté vacío.
- El feed de precios se actualiza automáticamente cada ~30 segundos.
- Recomendación: tras cambios de tema o de vista, recarga con **Ctrl+F5** para forzar recursos nuevos.

---

## 3. Pantalla Principal

### 3.1 Vista general

El panel es una sola pantalla a lo ancho, con (de arriba abajo):

1. **Cabecera**: título, estado del feed (proveedor activo, ticks, BD, último dato), **selector de
   proveedor**, botón de **tema** (claro/oscuro) y botón **👁 Visor**.
2. **Tarjetas de resumen** (6): Valor de cuenta, Efectivo, Valor de cartera, PnL total, Posiciones,
   Winrate.
3. **Gráfico de precios** con su barra de rango y controles de vista.
4. **Gráfico de evolución del valor de cuenta**.
5. **Métricas avanzadas** y **Resultados por periodo**.
6. **Tablas** de posiciones abiertas y trades cerrados.
7. **Botones de acción**: Buscar/añadir instrumento, Caja, Dividendos, Importar CSV, Nueva posición.

### 3.2 Elementos de la interfaz

| Elemento | Qué muestra |
|---|---|
| **Valor de cuenta** *(→ glosario)* | Efectivo + valor de la cartera, convertido a divisa base (EUR). Subtítulo: rentabilidad % sobre lo aportado. |
| **Efectivo** | Caja disponible (aportado − invertido + realizado − comisiones). |
| **Valor de cartera** | Suma del valor de mercado de las posiciones abiertas. |
| **PnL total** *(→ glosario)* | Ganancia/pérdida total (realizada + no realizada), neta de comisiones y con dividendos. |
| **Posiciones** | Nº de posiciones abiertas. |
| **Winrate** *(→ glosario)* | % de trades cerrados con ganancia. |

Los importes se muestran en la **divisa de cada instrumento** *(→ glosario: divisa base)* y, cuando
difiere de la base (EUR), también su equivalente aproximado `≈ €`.

### 3.3 Navegación

Todo ocurre en la misma página (no hay menús). Las acciones que **modifican datos** se abren en
**ventanas modales** (Caja, Dividendos, Importar CSV, Nueva posición, Buscar). Los controles de
**vista** (rango, símbolo, velas, margen…) actúan sobre los gráficos sin recargar.

---

## 4. Funcionalidades

### 4.1 Buscar y seguir instrumentos (watchlist)

1. Pulsa **🔎 Buscar / añadir instrumento**.
2. Escribe nombre, **ISIN** o **ticker** (p. ej. `apple`, `US0378331005`, `AAPL`).
3. En los resultados, pulsa **+ Añadir** para incorporarlo a la **watchlist**.
4. Para dejar de seguir un símbolo, usa la **✕** junto a su nombre.

> El buscador usa el **proveedor activo**. Cada proveedor tiene su convención de símbolos (p. ej.
> SK hynix en Frankfurt es `HY9H.F` en Yahoo y `HY9H` en Twelve Data). **WKN no está soportado**.

### 4.2 Selector de proveedor de datos

En la cabecera puedes cambiar en caliente entre **Yahoo Finance**, **Twelve Data** y **Alpha
Vantage**. Yahoo funciona sin API key; Twelve Data y Alpha Vantage requieren **API key** (se
configura en *user-secrets*, no en el repositorio) y tienen límites de uso en su plan gratuito. Si
falta la key, aparece el aviso «⚠ sin API key».

### 4.3 Gráfico de precios

- **Barra de rango**: `1D`, `5D`, `1M`, `3M`, `6M`, `YTD` *(→ glosario)*, `1A`, `3A`, `5A`.
- **Eje sin huecos**: los fines de semana y festivos no dejan espacios en blanco.
- **🕯️ Velas**: alterna entre **línea** y **velas japonesas** *(→ glosario: OHLC)*. Solo con un único
  símbolo seleccionado.
- **Volumen** *(→ glosario)*: barras verde/rojo bajo el precio (verde si sube, rojo si baja).
- **Techo/suelo**: líneas discontinuas con el máximo y mínimo del rango.
- **Margen del eje Y**: selector Ajustado / ±1 % / ±2 % / ±5 % / ±10 % / ±20 % (compresión visual).
- **Variación del rango**: en la cabecera del gráfico, el cambio acumulado (importe y %) del rango
  mostrado, coloreado.

### 4.4 Señales ML (orientativas)

El switch **🔮 Señales ML** activa dos indicadores educativos sobre el símbolo seleccionado:

- **🔮 Pronóstico (SSA)** *(→ glosario)*: proyección del precio a N periodos con banda de confianza y
  señal Alcista/Bajista/Neutral. Dibuja una línea y banda sobre el gráfico.
- **📊 Clasificación (sube/baja)** *(→ glosario: SDCA, FastTree)*: probabilidad de que suba el próximo
  periodo → señal Comprar/Vender/Mantener. El tooltip muestra la calidad del modelo (accuracy/AUC).

> **Honestidad**: la dirección del precio ronda el azar; la calidad suele estar entre 0,50 y 0,65.
> Es un **indicador educativo, no asesoramiento**. Con histórico insuficiente muestra «Insuficiente».

### 4.5 Posiciones (tu cartera real)

- **➕ Nueva posición**: símbolo (con autocompletado de la watchlist), precio de entrada, cantidad y
  fecha. Al crearla, el símbolo se añade a la watchlist automáticamente.
- **Cerrar**: registra el precio de salida (por defecto, el precio actual) y calcula el **PnL
  realizado** *(→ glosario)*.
- **Eliminar**: borra la posición.
- Cada fila muestra el **PnL en vivo**, la **rentabilidad %**, la **divisa** y (si difiere de la base)
  el equivalente en EUR.
- **Comisiones** *(→ glosario)*: se aplica la tarifa del bróker al abrir y al cerrar (por defecto
  **Trade Republic: 1 € fijo por orden** → 2 € por operación completa).

### 4.6 Caja y dividendos

- **💰 Caja**: registra **aportaciones** e **retiradas**, cada una con su **divisa**. El «Aportado
  neto» se agrupa por divisa y se convierte a base.
- **💵 Dividendos**: registra los dividendos cobrados (símbolo, importe, divisa, fecha, nota). Suman al
  PnL realizado.

### 4.7 Evolución del valor de cuenta

Gráfico con la **curva del valor de cuenta** y la línea de **aportado neto**, con selector de rango
(1 día / 1 semana / 1 mes / Todo). Se toma un *snapshot* *(→ glosario)* automático periódicamente.

### 4.8 Métricas avanzadas

Tarjeta con **Max Drawdown** *(→ glosario)* (y actual), **Sharpe** *(→ glosario)*, **volatilidad**
anualizada y **Profit Factor** *(→ glosario)*. Las métricas de rendimiento se calculan sobre el
**índice de retorno** (neutral a aportaciones/retiradas). Sharpe y volatilidad necesitan **≥ ~11 días**
de datos; con menos, muestran «insuficiente» (anualizar pocas muestras da valores absurdos).

### 4.9 Resultados por periodo

Tabla de **trades cerrados agrupados por año y mes** según la fecha de cierre, con el **PnL sumado**
(convertido a divisa base, neto de comisiones y con dividendos), nº de trades y ganados/perdidos.

### 4.10 Modo Visor (solo lectura)

El botón **👁 Visor** pone el panel en **solo lectura**: oculta los controles que modifican datos
(proveedor, buscar/añadir, Caja, Dividendos, Importar CSV, Nueva posición, quitar símbolo y la columna
Acciones), conservando toda la visualización. Ideal para **mostrar en pantalla** sin riesgo de tocar
nada. La preferencia se recuerda entre sesiones.

---

## 5. Operaciones Frecuentes

### 5.1 Empezar a seguir un instrumento nuevo

`🔎 Buscar / añadir` → escribe el nombre/ISIN/ticker → **+ Añadir**. Aparecerá en la watchlist y su
precio se actualizará en cada ciclo.

### 5.2 Registrar una compra real

`➕ Nueva posición` → símbolo, precio de entrada, cantidad, fecha → **Guardar**. Verás su PnL en vivo
frente al precio real.

### 5.3 Cerrar una posición

En la tabla de posiciones abiertas, fila → **Cerrar** → confirma el precio de salida. Pasa a «trades
cerrados» con su PnL realizado.

### 5.4 Aportar/retirar efectivo o registrar un dividendo

`💰 Caja` (aportación/retirada con divisa) o `💵 Dividendos` (importe cobrado). Los totales de cuenta
se recalculan al instante.

---

## 6. Informes y Exportación

### 6.1 Informes disponibles (en pantalla)

- Tarjetas de resumen, gráficos de precio y de cuenta, **métricas avanzadas** y **resultados por
  periodo** (año/mes).

### 6.2 Importar datos (CSV)

**📥 Importar CSV** permite dar de alta posiciones en bloque. Formato por fila:

```
symbol, entry, qty[, date[, exit[, closeDate]]]
```

- Solo `symbol, entry, qty` → **posición abierta**.
- Con `exit` (y opcionalmente `closeDate`) → **trade cerrado** (se abre y se cierra).
- Cabecera opcional; delimitador `,` o `;` (con `;` se admite coma decimal); varias fechas admitidas.
- Las filas con error se informan por línea y **no abortan** el resto.

> **No hay exportación** de datos en esta versión (los datos viven en `App_Data/trading.db`).

---

## 7. Configuración

> Al ser una app personal, el «administrador» eres tú. No hay gestión de usuarios.

### 7.1 Ajustes que se recuerdan entre sesiones

Se guardan en el navegador (localStorage): **tema** claro/oscuro, símbolo del gráfico, rango del
gráfico de precios, rango del gráfico de cuenta, tipo de gráfico (línea/velas), margen del eje Y y el
**modo Visor**.

### 7.2 Configuración del sistema (avanzada)

En `appsettings.json` / *user-secrets* del proyecto web:

- **`Broker:CommissionPerOrder`**: comisión por orden (por defecto `1,00` en divisa base — Trade
  Republic).
- **`Fx:BaseCurrency`** (EUR), `Fx:CacheMinutes`, `Fx:RefreshSeconds`: divisa base y refresco de tipos
  de cambio.
- **API keys** de Twelve Data / Alpha Vantage: en *user-secrets* (nunca en el repositorio).
- Intervalo del feed, retención de la BD, etc.

---

## 8. Preguntas Frecuentes (FAQ)

**¿Necesito Internet?** Sí. Los precios y los tipos de cambio (FX) se obtienen de proveedores externos
(Yahoo por defecto). Sin conexión, el panel no puede refrescar datos.

**¿Ejecuta operaciones reales o automáticas?** No. Es un **visor/tracker**. La estrategia automática
(MA Crossover) está **desactivada** (el código se conserva, pero no opera).

**¿Por qué un símbolo no da datos al cambiar de proveedor?** Cada proveedor usa su **convención de
símbolos**. Un símbolo estilo Yahoo (`HY9H.F`, `^GSPC`) puede no existir en Twelve Data/Alpha Vantage.
Busca el instrumento con el proveedor activo y añade **su** símbolo.

**¿Por qué las señales ML dicen «Insuficiente»?** Falta histórico suficiente para entrenar (p. ej. en
rango 1D o símbolos muy nuevos). Prueba un rango mayor (6M, 1A).

**¿Por qué las métricas Sharpe/volatilidad no aparecen?** Necesitan **≥ ~11 días** de *snapshots*; con
menos, no son fiables y se ocultan a propósito.

**¿En qué divisa se ven los totales?** En la **divisa base** (EUR). Cada fila se muestra en la divisa
nativa del instrumento y, si difiere, con su equivalente `≈ €`.

## 9. Solución de Problemas

### 9.1 La página no carga
Verifica que `dotnet run` sigue activo y usa la URL/puerto que muestra la consola. Acepta el
certificado de desarrollo si el navegador lo pide.

### 9.2 No veo los cambios de estilo/gráfico
Recarga con **Ctrl+F5** (fuerza recarga de JS/CSS cacheados).

### 9.3 Un símbolo aparece sin precio / se omite cada ciclo
Suele ser un símbolo no disponible en el proveedor/plan actual. Cámbialo por el símbolo correcto del
proveedor activo, o vuelve a **Yahoo Finance** (cubre la watchlist habitual sin API key).

### 9.4 Errores al importar CSV
Revisa el formato de la fila señalada (precio de salida inválido, fecha de cierre anterior a la de
apertura…). Las filas correctas se importan igualmente.

---

## 10. Soporte y Contacto

Proyecto **personal** (fuera del ámbito de soporte STIC de Comillas). No hay canal de soporte formal
ni SLA. El repositorio y su historial (`_duran/`) son la referencia de funcionamiento.

---

## Anexos

### A. Atajos / gestos útiles
- **Ctrl+F5**: recarga forzada tras cambios de JS/CSS.
- **👁 Visor**: alterna solo-lectura (persistente).
- Clic en la barra de rango: cambia el periodo del gráfico (se recuerda).

### B. Glosario
Consulta el **[Glosario de términos](GLOSARIO.md)** para las definiciones de todos los términos de
negocio y técnicos usados en este manual.

---

## Historial de Cambios de la Aplicación (resumen)

- **1.39.0** — Integración de SonarAnalyzer (calidad de código). Sin cambios funcionales.
- **1.37.0** — Comisiones (Trade Republic) y dividendos en el PnL.
- **1.36.0** — Resultados por periodo (año/mes).
- **1.35.0** — Métricas avanzadas (drawdown, Sharpe, volatilidad, profit factor).
- **1.30.0–1.32.0** — Señales ML (SSA + clasificación sube/baja).
- **1.29.0** — Tercer proveedor (Alpha Vantage).
- **Fase pivote** — De simulador a **tracker de precios reales**: buscador, watchlist, posiciones,
  caja, feed 100 % real (auto-trading desactivado).

> El detalle completo está en `_duran/HISTORIAL_CAMBIOS.md` y `_duran/FUNCIONALIDADES.md`.
