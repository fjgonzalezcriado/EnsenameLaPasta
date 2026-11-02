(function () {
    'use strict';

    const REFRESH_MS = 3000;
    // Margen del eje Y por encima del techo y por debajo del suelo, como fracción del precio
    // (HV-039 fijo → HV-040 seleccionable). 0 = ajustado (máxima extensión); mayor = más
    // compresión. Relativo al precio para que funcione igual en cualquier instrumento.
    const Y_MARGIN_ALLOWED = [0, 0.01, 0.02, 0.05, 0.10, 0.20];
    let viewerMode = false;          // modo visor puro: solo lectura, sin controles de edición (HV-046)
    let privacyMode = false;         // modo privacidad: desenfoca los importes sensibles (HV-051)
    const FORECAST_HORIZON = 10;
    const COLORS = ['#0d6efd', '#fd7e14', '#198754', '#dc3545', '#6f42c1', '#20c997'];
    // En modo "En vivo" los ticks se capturan cada ~30 s, pero entre sesiones (app apagada)
    // hay huecos de horas/días. Si dos ticks consecutivos distan más de esto, cortamos la
    // línea (punto nulo) para no dibujar una diagonal recta engañosa sobre el hueco.
    const LIVE_GAP_MS = 5 * 60 * 1000;
    let lastSeries = [];         // última priceSeries recibida del feed en vivo
    let historyPoints = 50;      // nº de puntos de histórico a pedir por símbolo
    const HISTORY_ALLOWED = [50, 250, 1000, 5000];
    let baseCurrency = 'EUR';    // divisa base de los totales (HV-020); para el PnL convertido por fila (HV-022)

    // ── Ventanas de "Evolución de precios" (HV-054) ─────────────────────────
    // El gráfico dejó de ser único: ahora se abre una ventana independiente por símbolo.
    // Cada ventana lleva SU PROPIO estado (rango, línea/velas, margen del eje Y, señales ML)
    // y su propia instancia de Chart.js, así que lo que antes eran variables de módulo
    // (chartMode, chartType, yMarginPct, forecastActive, lastForecast, lastSignal,
    // lastRenderedSeries, priceChart) vive ahora dentro de cada objeto "win".
    const RANGES = ['1D', '5D', '1M', '3M', '6M', 'YTD', '1A', '3A', '5A'];
    const PRICE_WINDOWS_KEY = 'priceWindows';
    const MAX_PRICE_WINDOWS = 8;   // tope de cordura: cada ventana = 1 petición de histórico
    const priceWindows = [];
    let winSeq = 0;                // contador para generar ids únicos de ventana
    // Divisa de cotización por símbolo (HV-037): se alimenta de posiciones (openTrades) y de la
    // watchlist (/api/instruments/tracked). Se usa para mostrar la variación del rango en su moneda.
    const symbolCurrency = {};
    // Símbolos con el detalle de lotes desplegado (HV-053). renderOpenTrades reconstruye el tbody
    // en cada refresco (polling), así que el estado de expansión debe vivir fuera del DOM o se
    // pierde y la fila se "auto-colapsa" al siguiente refresco.
    const expandedGroups = new Set();

    // ── Formateadores (es-ES) ──────────────────────────────────────────────
    const EUR = new Intl.NumberFormat('es-ES', { style: 'currency', currency: 'EUR' });
    const NUM = new Intl.NumberFormat('es-ES', { minimumFractionDigits: 2, maximumFractionDigits: 4 });
    const PRICE = new Intl.NumberFormat('es-ES', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

    function eur(v) { return EUR.format(Number(v) || 0); }
    function num(v) { return NUM.format(Number(v) || 0); }
    // Formatea un importe en la divisa del instrumento (EUR, USD…). Si no se
    // conoce la divisa, devuelve el número sin símbolo (evita implicar €).
    const _ccyFormatters = {};
    function money(v, currency) {
        const n = Number(v) || 0;
        const cur = (currency || '').trim().toUpperCase();
        if (cur.length === 3) {
            try {
                if (!_ccyFormatters[cur]) {
                    _ccyFormatters[cur] = new Intl.NumberFormat('es-ES', { style: 'currency', currency: cur });
                }
                return _ccyFormatters[cur].format(n);
            } catch (e) { /* código de divisa no válido → fallback */ }
        }
        return num(n);
    }
    function priceFmt(v) { return PRICE.format(Number(v) || 0); }
    const VOL = new Intl.NumberFormat('es-ES', { maximumFractionDigits: 0 });
    function volFmt(v) { return VOL.format(Number(v) || 0); }
    // #rrggbb -> rgba(r,g,b,a) para las barras de volumen (color de la serie, tenue).
    function hexToRgba(hex, a) {
        const m = /^#?([0-9a-f]{6})$/i.exec(String(hex || ''));
        if (!m) return 'rgba(108,117,125,' + a + ')';
        const n = parseInt(m[1], 16);
        return 'rgba(' + ((n >> 16) & 255) + ',' + ((n >> 8) & 255) + ',' + (n & 255) + ',' + a + ')';
    }
    function fmtBytes(b) {
        b = Number(b) || 0;
        if (b >= 1048576) return (b / 1048576).toFixed(1) + ' MB';
        if (b >= 1024) return Math.round(b / 1024) + ' KB';
        return b + ' B';
    }

    // Colores del gráfico según el tema (claro/oscuro). Como funciones scriptables,
    // Chart.js las reevalúa en cada dibujado → se adaptan al cambiar de tema.
    function isDarkTheme() { return document.documentElement.getAttribute('data-bs-theme') === 'dark'; }
    function gridColor() { return isDarkTheme() ? 'rgba(255,255,255,0.10)' : 'rgba(0,0,0,0.08)'; }
    function axisTextColor() { return isDarkTheme() ? 'rgba(233,241,255,0.75)' : 'rgba(33,37,41,0.75)'; }
    function pctSigned(v) { const n = Number(v) || 0; return (n >= 0 ? '+' : '') + n.toFixed(2) + '%'; }
    // Celda de PnL: importe en su divisa y, si difiere de la base, el equivalente convertido (HV-022).
    function pnlCell(nativeVal, currency, baseVal) {
        const cur = (currency || baseCurrency).toUpperCase();
        let html = money(nativeVal, currency);
        if (cur !== baseCurrency.toUpperCase()) {
            html += ' <small class="text-muted">≈ ' + money(baseVal, baseCurrency) + '</small>';
        }
        return html;
    }
    function signClass(v) { return (Number(v) || 0) >= 0 ? 'text-success' : 'text-danger'; }

    function setSigned(elId, value, formatter) {
        const el = document.getElementById(elId);
        if (!el) return;
        el.textContent = formatter(value);
        el.classList.remove('text-success', 'text-danger');
        el.classList.add(signClass(value));
    }

    // ── Estado del feed (proveedor + en vivo) ───────────────────────────────
    function renderStatus(d) {
        // Sincroniza el selector de proveedor con el activo (sin pisar una selección en curso).
        const provSel = document.getElementById('providerSelect');
        if (provSel && d.providerType && document.activeElement !== provSel && provSel.value !== d.providerType) {
            provSel.value = d.providerType;
        }
        const tickCount = document.getElementById('tickCount');
        if (tickCount) tickCount.textContent = new Intl.NumberFormat('es-ES').format(d.totalTicks || 0);
        const dbSize = document.getElementById('dbSize');
        if (dbSize) dbSize.textContent = fmtBytes(d.databaseSizeBytes);
        const lastTick = document.getElementById('lastTick');
        if (lastTick) lastTick.textContent = d.lastTickUtc ? new Date(d.lastTickUtc).toLocaleTimeString('es-ES') : '—';
    }

    function renderMetrics(d) {
        if (d.baseCurrency) baseCurrency = d.baseCurrency;
        const baseCcy = document.getElementById('baseCcy');
        if (baseCcy && d.baseCurrency) baseCcy.textContent = d.baseCurrency;
        document.getElementById('accountValue').textContent = eur(d.accountValue);
        setSigned('returnPct', d.returnPct, pctSigned);
        setSigned('cash', d.cash, eur);
        document.getElementById('netDeposits').textContent = eur(d.netDeposits);
        document.getElementById('marketValue').textContent = eur(d.marketValue);
        document.getElementById('invested').textContent = eur(d.invested);
        setSigned('realizedPnL', d.realizedPnL, eur);
        setSigned('unrealizedPnL', d.unrealizedPnL, eur);
        setSigned('totalPnL', d.totalPnL, eur);
        document.getElementById('openPositions').textContent = d.openPositions;
        document.getElementById('closedTrades').textContent = d.closedTrades;
        document.getElementById('winrate').textContent = (Number(d.winrate) || 0).toFixed(2) + '%';
    }

    // Fila de una compra individual. isDetail=true → fila hija de un grupo (indentada); visible solo
    // si ese grupo está expandido (por defecto oculta).
    function openTradeRow(t, isDetail, groupSymbol, expanded) {
        const rowAttrs = isDetail
            ? (' class="table-light js-lot-detail" data-group="' + groupSymbol + '" style="display:' + (expanded ? '' : 'none') + '"')
            : '';
        const symbolCell = isDetail
            ? '<td class="ps-4"><small class="text-muted">↳ ' + t.symbol + '</small></td>'
            : '<td><strong>' + t.symbol + '</strong></td>';
        return '<tr' + rowAttrs + '>'
            + symbolCell
            + '<td><span class="badge text-bg-secondary">' + (t.currency || '—') + '</span></td>'
            + '<td>' + num(t.entryPrice) + '</td>'
            + '<td>' + num(t.currentPrice) + '</td>'
            + '<td>' + num(t.quantity) + '</td>'
            + '<td class="' + signClass(t.unrealizedPnL) + '">' + pnlCell(t.unrealizedPnL, t.currency, t.unrealizedPnLBase) + '</td>'
            + '<td class="' + signClass(t.returnPct) + '">' + pctSigned(t.returnPct) + '</td>'
            + '<td><small>' + new Date(t.createdAt).toLocaleString('es-ES') + '</small></td>'
            + (viewerMode ? '' : ('<td class="text-end text-nowrap">'
                + '<button type="button" class="btn btn-outline-primary btn-sm py-0 me-1" '
                + 'data-close="' + t.id + '" data-symbol="' + t.symbol + '" data-price="' + t.currentPrice + '">Cerrar</button>'
                + '<button type="button" class="btn btn-outline-danger btn-sm py-0" '
                + 'data-del="' + t.id + '" data-symbol="' + t.symbol + '" title="Eliminar del seguimiento">✕</button>'
                + '</td>'))
            + '</tr>';
    }

    // Varias compras del mismo símbolo se agrupan en una única fila con precio de entrada
    // medio ponderado (igual que el bróker), con el detalle de cada lote expandible (HV-053).
    function renderOpenTrades(trades) {
        (trades || []).forEach(function (t) { if (t.currency) symbolCurrency[t.symbol] = t.currency; });
        const tbody = document.getElementById('openTradesBody');
        if (!trades || trades.length === 0) {
            tbody.innerHTML = '<tr><td colspan="' + (viewerMode ? 8 : 9) + '" class="text-center text-muted">Sin posiciones abiertas</td></tr>';
            return;
        }

        const groups = [];
        const bySymbol = {};
        trades.forEach(function (t) {
            let g = bySymbol[t.symbol];
            if (!g) {
                g = {
                    symbol: t.symbol, currency: t.currency, quantity: 0, cost: 0,
                    unrealizedPnL: 0, unrealizedPnLBase: 0, createdAt: t.createdAt,
                    currentPrice: t.currentPrice, lots: []
                };
                bySymbol[t.symbol] = g;
                groups.push(g);
            }
            g.quantity += Number(t.quantity) || 0;
            g.cost += (Number(t.entryPrice) || 0) * (Number(t.quantity) || 0);
            g.unrealizedPnL += Number(t.unrealizedPnL) || 0;
            g.unrealizedPnLBase += Number(t.unrealizedPnLBase) || 0;
            g.currentPrice = t.currentPrice;
            if (new Date(t.createdAt) < new Date(g.createdAt)) g.createdAt = t.createdAt;
            g.lots.push(t);
        });

        tbody.innerHTML = groups.map(function (g) {
            if (g.lots.length === 1) return openTradeRow(g.lots[0], false);

            const avgEntry = g.quantity !== 0 ? g.cost / g.quantity : 0;
            const pct = avgEntry !== 0 ? ((g.currentPrice - avgEntry) / avgEntry * 100) : 0;
            const expanded = expandedGroups.has(g.symbol);

            const summaryRow = '<tr class="js-lot-toggle" data-toggle-group="' + g.symbol + '" data-expanded="' + (expanded ? '1' : '0') + '" style="cursor:pointer">'
                + '<td><span class="js-lot-caret me-1">' + (expanded ? '▾' : '▸') + '</span><strong>' + g.symbol + '</strong> '
                + '<span class="badge text-bg-light text-muted border">' + g.lots.length + ' lotes</span></td>'
                + '<td><span class="badge text-bg-secondary">' + (g.currency || '—') + '</span></td>'
                + '<td>' + num(avgEntry) + ' <small class="text-muted">(prom.)</small></td>'
                + '<td>' + num(g.currentPrice) + '</td>'
                + '<td>' + num(g.quantity) + '</td>'
                + '<td class="' + signClass(g.unrealizedPnL) + '">' + pnlCell(g.unrealizedPnL, g.currency, g.unrealizedPnLBase) + '</td>'
                + '<td class="' + signClass(pct) + '">' + pctSigned(pct) + '</td>'
                + '<td><small>' + new Date(g.createdAt).toLocaleString('es-ES') + '</small></td>'
                + (viewerMode ? '' : '<td class="text-end text-nowrap"><small class="text-muted">ver detalle</small></td>')
                + '</tr>';

            const detailRows = g.lots.map(function (t) { return openTradeRow(t, true, g.symbol, expanded); }).join('');
            return summaryRow + detailRows;
        }).join('');
    }

    function renderClosedTrades(trades) {
        (trades || []).forEach(function (t) { if (t.currency) symbolCurrency[t.symbol] = t.currency; });
        const tbody = document.getElementById('closedTradesBody');
        if (!trades || trades.length === 0) {
            tbody.innerHTML = '<tr><td colspan="7" class="text-center text-muted">Sin trades cerrados</td></tr>';
            return;
        }
        tbody.innerHTML = trades.map(function (t) {
            return '<tr>'
                + '<td><strong>' + t.symbol + '</strong></td>'
                + '<td><span class="badge text-bg-secondary">' + (t.currency || '—') + '</span></td>'
                + '<td>' + num(t.entryPrice) + ' → ' + num(t.exitPrice) + '</td>'
                + '<td>' + num(t.quantity) + '</td>'
                + '<td class="' + signClass(t.realizedPnL) + '">' + pnlCell(t.realizedPnL, t.currency, t.realizedPnLBase) + '</td>'
                + '<td class="' + signClass(t.returnPct) + '">' + pctSigned(t.returnPct) + '</td>'
                + '<td><small>' + new Date(t.closedAt).toLocaleString('es-ES') + '</small></td>'
                + '</tr>';
        }).join('');
    }

    // Plugin: etiqueta del último valor de cada serie dibujada SOBRE el eje
    // derecho (estilo "último precio" de TradingView), con flechita que apunta
    // al nivel del precio desde el eje.
    const currentValuePlugin = {
        id: 'currentValueLabels',
        afterDatasetsDraw: function (chart) {
            const ctx = chart.ctx;
            const area = chart.chartArea;
            const axisLeft = area.right;          // borde derecho del área de trazado = inicio del eje Y (derecha)
            const axisRight = chart.width;        // borde derecho del canvas
            const h = 18;
            chart.data.datasets.forEach(function (ds, i) {
                if (ds.isVolume || ds.isForecast) return;   // no aplica a volumen ni a pronóstico
                const meta = chart.getDatasetMeta(i);
                if (meta.hidden || !meta.data || meta.data.length === 0) return;
                // Último punto con valor. Con el eje de categorías (HV-038) los datos son
                // numéricos y pueden acabar en null (huecos), así que buscamos el último válido.
                let li = -1;
                for (let k = ds.data.length - 1; k >= 0; k--) {
                    const v = ds.data[k];
                    const yv = (v && typeof v === 'object') ? v.y : v;
                    if (v !== null && v !== undefined && Number.isFinite(Number(yv))) { li = k; break; }
                }
                if (li < 0) return;
                const last = meta.data[li];
                const rawv = ds.data[li];
                const yval = (rawv && typeof rawv === 'object') ? rawv.y : rawv;
                if (!last || !Number.isFinite(last.y)) return;

                const text = priceFmt(yval);
                const y = Math.max(area.top + h / 2, Math.min(area.bottom - h / 2, last.y));
                const w = Math.max(axisRight - axisLeft - 1, ctx.measureText(text).width + 8);

                ctx.save();
                ctx.font = '600 11px sans-serif';
                // En velas la línea es transparente; usa $valueColor para la etiqueta (HV-041).
                ctx.fillStyle = ds.$valueColor || ds.borderColor;

                // Flechita apuntando al nivel de precio (hacia la izquierda, dentro del margen).
                ctx.beginPath();
                ctx.moveTo(axisLeft, y - 4);
                ctx.lineTo(axisLeft - 6, y);
                ctx.lineTo(axisLeft, y + 4);
                ctx.closePath();
                ctx.fill();

                // Caja sobre el eje.
                if (typeof ctx.roundRect === 'function') {
                    ctx.beginPath();
                    ctx.roundRect(axisLeft, y - h / 2, w, h, 2);
                    ctx.fill();
                } else {
                    ctx.fillRect(axisLeft, y - h / 2, w, h);
                }

                // Valor centrado en la caja del eje.
                ctx.fillStyle = '#fff';
                ctx.textBaseline = 'middle';
                ctx.textAlign = 'center';
                ctx.fillText(text, axisLeft + w / 2, y);
                ctx.restore();
            });
        }
    };

    // Plugin: velas japonesas (HV-041). Dibuja, por categoría, la mecha (máx→mín) y el cuerpo
    // (apertura↔cierre) en verde (cierre≥apertura) o rojo. Lee chart.$ohlc (alineado a labels)
    // y solo actúa si chart.$candleMode. Usa el eje de categorías, así respeta HV-038 (sin huecos).
    const candlePlugin = {
        id: 'candles',
        afterDatasetsDraw: function (chart) {
            if (!chart.$candleMode) return;
            const oc = chart.$ohlc;
            const x = chart.scales.x, y = chart.scales.y, area = chart.chartArea, ctx = chart.ctx;
            if (!oc || !x || !y || !area) return;
            const n = oc.length;
            const slot = n > 1 ? Math.abs(x.getPixelForValue(1) - x.getPixelForValue(0)) : (area.right - area.left);
            const bw = Math.max(1, Math.min(16, slot * 0.6));
            const UP = '#198754', DOWN = '#dc3545';
            ctx.save();
            for (let i = 0; i < n; i++) {
                const d = oc[i];
                if (!d) continue;
                const cx = x.getPixelForValue(i);
                const col = d.c >= d.o ? UP : DOWN;
                ctx.strokeStyle = col; ctx.fillStyle = col; ctx.lineWidth = 1;
                // Mecha (máximo → mínimo).
                ctx.beginPath();
                ctx.moveTo(cx, y.getPixelForValue(d.h));
                ctx.lineTo(cx, y.getPixelForValue(d.l));
                ctx.stroke();
                // Cuerpo (apertura ↔ cierre); altura mínima 1px para que un doji sea visible.
                const yo = y.getPixelForValue(d.o), yc = y.getPixelForValue(d.c);
                const top = Math.min(yo, yc), bh = Math.max(1, Math.abs(yc - yo));
                ctx.fillRect(cx - bw / 2, top, bw, bh);
            }
            ctx.restore();
        }
    };

    // Plugin: líneas discontinuas en el techo (máximo) y el suelo (mínimo) del rango (HV-039).
    // Lee chart.$hiLo = { high, low } (fijado en renderChart) y las dibuja de lado a lado.
    const highLowLinesPlugin = {
        id: 'highLowLines',
        afterDatasetsDraw: function (chart) {
            const hl = chart.$hiLo;
            if (!hl || !Number.isFinite(hl.high) || !Number.isFinite(hl.low)) return;
            const y = chart.scales.y;
            const area = chart.chartArea;
            if (!y || !area) return;
            const ctx = chart.ctx;
            const color = isDarkTheme() ? 'rgba(233,241,255,0.55)' : 'rgba(33,37,41,0.50)';
            [['Techo', hl.high, 'bottom'], ['Suelo', hl.low, 'top']].forEach(function (row) {
                const py = y.getPixelForValue(row[1]);
                if (!Number.isFinite(py)) return;
                ctx.save();
                ctx.strokeStyle = color;
                ctx.lineWidth = 1;
                ctx.setLineDash([6, 4]);
                ctx.beginPath();
                ctx.moveTo(area.left, py);
                ctx.lineTo(area.right, py);
                ctx.stroke();
                ctx.setLineDash([]);
                ctx.font = '600 10px sans-serif';
                ctx.fillStyle = color;
                ctx.textBaseline = row[2];
                ctx.textAlign = 'left';
                ctx.fillText(row[0] + ' ' + priceFmt(row[1]), area.left + 4, row[2] === 'bottom' ? py - 2 : py + 2);
                ctx.restore();
            });
        }
    };

    // Margen derecho: extiende el max del eje X ~12% para que la línea no quede
    // pegada al borde y se distinga el valor actual.
    function computeXBounds(datasets) {
        let minX = Infinity, maxX = -Infinity;
        datasets.forEach(function (ds) {
            ds.data.forEach(function (pt) {
                if (pt.x < minX) minX = pt.x;
                if (pt.x > maxX) maxX = pt.x;
            });
        });
        if (!Number.isFinite(minX) || !Number.isFinite(maxX)) return null;
        const span = maxX - minX;
        const pad = span > 0 ? span * 0.12 : 60000;
        return { min: minX, max: maxX + pad };
    }

    const DISPLAY_FORMATS = {
        second: 'HH:mm:ss', minute: 'HH:mm', hour: 'dd MMM HH:mm',
        day: 'dd MMM', month: 'MMM yyyy', year: 'yyyy'
    };

    // Unidad del eje X según el span temporal (para que 1D y 5A se vean bien).
    function timeUnitFor(xb) {
        const span = xb ? (xb.max - xb.min) : 0;
        const D = 86400000;
        if (span > 730 * D) return 'year';
        if (span > 60 * D) return 'month';
        if (span > 2 * D) return 'day';
        if (span > 2 * 3600000) return 'hour';
        if (span > 2 * 60000) return 'minute';
        return 'second';
    }

    // Mantiene el <select> de símbolos sincronizado con las series disponibles.
    // Combo "+ Nueva ventana": lista los símbolos seguidos que todavía no tienen ventana
    // abierta (HV-054). Se repuebla en cada ciclo de datos, así que un símbolo recién
    // añadido aparece solo, y uno con ventana abierta desaparece de la lista.
    function ensureSymbolOptions(series) {
        const sel = document.getElementById('newPriceWindowSelect');
        if (!sel) return;
        const symbols = (series || []).map(function (s) { return s.symbol; });
        const open = priceWindows.map(function (w) { return w.symbol; });
        const free = symbols.filter(function (s) { return open.indexOf(s) === -1; });
        const want = [''].concat(free);
        const have = Array.from(sel.options).map(function (o) { return o.value; });
        if (have.join('|') === want.join('|')) return;   // sin cambios: no tocar el DOM
        sel.innerHTML = '<option value="">' + (free.length ? '-- Selecciona --' : '-- Sin símbolos libres --') + '</option>'
            + free.map(function (s) { return '<option value="' + escapeHtml(s) + '">' + escapeHtml(s) + '</option>'; }).join('');
        sel.value = '';
    }

    // Inserta un punto nulo entre puntos separados por más de gapMs para que Chart.js
    // corte la línea sobre los huecos entre sesiones (evita la diagonal/compresión falsa).
    function insertGaps(points, gapMs) {
        if (points.length < 2) return points;
        const out = [];
        for (let i = 0; i < points.length; i++) {
            if (i > 0 && (points[i].x - points[i - 1].x) > gapMs) {
                out.push({ x: points[i - 1].x + Math.floor((points[i].x - points[i - 1].x) / 2), y: null });
            }
            out.push(points[i]);
        }
        return out;
    }
    function insertLiveGaps(points) { return insertGaps(points, LIVE_GAP_MS); }

    // Formateadores de etiquetas del eje de categorías (HV-038), elegidos por el span total.
    // 'short' para el tick del eje; 'full' para el título del tooltip.
    function labelFormatterFor(spanMs) {
        const D = 86400000;
        const mk = function (opts) { const f = new Intl.DateTimeFormat('es-ES', opts); return function (ms) { return f.format(new Date(ms)); }; };
        if (spanMs <= 2 * D) {   // intradía (1D): hora
            return { short: mk({ hour: '2-digit', minute: '2-digit' }),
                     full: mk({ day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' }) };
        }
        if (spanMs <= 10 * D) {  // 5D (intradía multi-día): día + hora
            const s = mk({ day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' });
            return { short: s, full: s };
        }
        if (spanMs <= 400 * D) { // diario: día y mes
            return { short: mk({ day: '2-digit', month: 'short' }),
                     full: mk({ day: '2-digit', month: 'short', year: 'numeric' }) };
        }
        const s = mk({ month: 'short', year: 'numeric' });   // semanal/largo: mes y año
        return { short: s, full: s };
    }

    // Variación acumulada del rango mostrado (HV-037): primer→último cierre de la serie
    // del símbolo seleccionado. Muestra el importe en la divisa del instrumento y el %.
    function updateChartDelta(win, series) {
        const el = win.els.delta;
        if (!el) return;
        const list = series || [];
        const s = list.find(function (x) { return x.symbol === win.symbol; }) || null;
        const pts = (s && s.points)
            ? s.points.map(function (p) { return Number(p.price); }).filter(function (v) { return Number.isFinite(v); })
            : [];
        if (!s || pts.length < 2) { el.textContent = ''; el.className = 'js-win-delta small fw-semibold'; return; }
        const last = pts[pts.length - 1];
        // En 1D la variación se calcula contra el cierre de ayer (HV-058), no contra la apertura
        // de hoy (pts[0] sería el primer tick de la sesión actual, no el cierre anterior). Para
        // el resto de rangos "primer punto vs último" dentro de la ventana sí es lo correcto.
        const baseline = (win.range === '1D' && s.previousClose > 0) ? Number(s.previousClose) : pts[0];
        const delta = last - baseline;
        const pct = baseline !== 0 ? (delta / baseline * 100) : 0;
        const cur = symbolCurrency[s.symbol] || '';
        const sign = delta > 0 ? '+' : '';   // money() ya antepone '−' en negativos
        el.textContent = win.range + ': ' + sign + money(delta, cur) + ' (' + pctSigned(pct) + ')';
        el.className = 'js-win-delta small fw-semibold ' + signClass(delta);
    }

    function renderChart(win, series) {
        if (typeof Chart === 'undefined') {
            console.error('[dashboard] Chart.js no está cargado (¿CDN bloqueado o sin conexión?).');
            return;
        }
        win.lastSeries = series;   // para re-render al cambiar margen/velas sin refetch (HV-040/041)
        updateChartDelta(win, series);
        const chartMode = win.range;
        const chartType = win.chartType;
        const yMarginPct = win.yMarginPct;
        const selectedSymbol = win.symbol;
        const forecastActive = win.forecastActive;
        const lastForecast = win.forecast;

        // Eje de CATEGORÍAS (HV-038): las X son los instantes de cotización, sin huecos de
        // fin de semana/festivos/nocturnos (el eje de tiempo los dejaba en blanco y unía los
        // puntos con una diagonal recta engañosa). Para rangos diarios/semanales agrupamos por
        // día (UTC) para que series de distintas bolsas casen en la misma fecha.
        const DAILY_RANGES = ['1M', '3M', '6M', 'YTD', '1A', '3A', '5A'];
        const snapDay = DAILY_RANGES.indexOf(chartMode) !== -1;
        const keyOf = function (t) { return snapDay ? Math.floor(t / 86400000) * 86400000 : t; };

        // Por serie: mapa clave(instante)->{ y: precio, v: volumen } + conjunto global de claves.
        const perSeries = [];
        const keySet = {};
        (series || []).forEach(function (s, idx) {
            if (s.symbol !== selectedSymbol) return;   // cada ventana pinta un único símbolo
            const map = {};
            (s.points || []).forEach(function (p) {
                const t = new Date(p.timestamp).getTime();
                const c = Number(p.price);   // cierre
                if (!Number.isFinite(t) || !Number.isFinite(c)) return;
                const k = keyOf(t);
                // OHLC para velas (HV-041); si falta un componente, cae al cierre (vela plana).
                const o = Number(p.open) > 0 ? Number(p.open) : c;
                const h = Number(p.high) > 0 ? Number(p.high) : c;
                const l = Number(p.low) > 0 ? Number(p.low) : c;
                map[k] = { y: c, v: Number(p.volume) || 0, o: o, h: h, l: l, c: c };
                keySet[k] = true;
            });
            if (Object.keys(map).length) perSeries.push({ symbol: s.symbol, map: map, idx: idx });
        });

        if (perSeries.length === 0) {
            console.warn('[dashboard] Sin puntos de precio para graficar todavía.');
            return;
        }

        // Categorías = instantes de cotización ordenados (sin fines de semana/festivos).
        const cats = Object.keys(keySet).map(Number).sort(function (a, b) { return a - b; });
        const span = cats.length > 1 ? (cats[cats.length - 1] - cats[0]) : 0;
        const fmt = labelFormatterFor(span);
        const labels = cats.map(function (t) { return fmt.short(t); });
        const labelsFull = cats.map(function (t) { return fmt.full(t); });

        // Modo velas (HV-041): solo con un único símbolo (varias velas superpuestas serían
        // ilegibles); con "Todos" cae a línea. Las velas las dibuja candlePlugin desde $ohlc.
        const candle = chartType === 'candle' && perSeries.length === 1;
        const ohlc = candle
            ? cats.map(function (t) { const e = perSeries[0].map[t]; return e ? { o: e.o, h: e.h, l: e.l, c: e.c } : null; })
            : null;

        // Datasets de precio + volumen alineados por índice de categoría. Un símbolo que no
        // cotice en una categoría concreta queda a null (spanGaps:false corta su línea ahí).
        const priceDatasets = [];
        const volumeDatasets = [];
        let maxVol = 0;
        perSeries.forEach(function (ps) {
            // El color lo fija la ventana (no el índice dentro de la serie): con una ventana
            // por símbolo el índice sería siempre 0 y todas saldrían del mismo color.
            const color = COLORS[win.colorIdx % COLORS.length];
            const priceArr = cats.map(function (t) { const e = ps.map[t]; return e ? e.y : null; });
            // En velas la línea se oculta (borderColor transparente) pero se conserva el dataset
            // para el escalado del eje y el hover; la etiqueta de valor usa $valueColor.
            priceDatasets.push({
                label: ps.symbol, data: priceArr, spanGaps: false,
                borderColor: candle ? 'transparent' : color, backgroundColor: 'transparent',
                $valueColor: color,
                tension: 0.1, pointRadius: 0, borderWidth: 2, yAxisID: 'y', order: 0
            });
            // Volumen: verde si el precio sube respecto a la cotización previa (alcista), rojo si baja.
            let prevY = null;
            const volArr = [];
            const volColors = [];
            cats.forEach(function (t) {
                const e = ps.map[t];
                if (e) {
                    if (e.v > maxVol) maxVol = e.v;
                    const up = prevY === null ? true : e.y >= prevY;
                    volColors.push(up ? hexToRgba('#198754', 0.5) : hexToRgba('#dc3545', 0.5));
                    volArr.push(e.v);
                    prevY = e.y;
                } else {
                    volArr.push(null);
                    volColors.push('rgba(0,0,0,0)');
                }
            });
            volumeDatasets.push({
                label: ps.symbol + ' · vol', data: volArr, type: 'bar', isVolume: true,
                yAxisID: 'yVol', backgroundColor: volColors, borderWidth: 0,
                order: 1, barPercentage: 1.0, categoryPercentage: 0.9, maxBarThickness: 10
            });
        });
        const datasets = priceDatasets.concat(volumeDatasets);

        // Overlay de pronóstico (HV-043): añade categorías futuras (+1..+N) con la línea de
        // pronóstico (naranja discontinua) y la banda de confianza sombreada. Solo con un símbolo.
        let fc = null;
        if (forecastActive && lastForecast && lastForecast.hasForecast
            && lastForecast.symbol === selectedSymbol && lastForecast.range === chartMode
            && Array.isArray(lastForecast.points) && lastForecast.points.length && priceDatasets.length === 1) {
            fc = lastForecast.points;
        }
        if (fc) {
            const h = fc.length;
            const baseLen = cats.length;
            const realData = priceDatasets[0].data;
            let anchor = null;
            for (let k = realData.length - 1; k >= 0; k--) { if (realData[k] != null) { anchor = realData[k]; break; } }
            for (let i = 1; i <= h; i++) { labels.push('+' + i); labelsFull.push('Pronóstico +' + i); }
            datasets.forEach(function (ds) { for (let i = 0; i < h; i++) ds.data.push(null); });
            const FORE = '#fd7e14';
            const arr = function (pick, withAnchor) {
                const a = new Array(baseLen + h).fill(null);
                if (withAnchor && anchor != null) a[baseLen - 1] = anchor;
                for (let i = 0; i < h; i++) a[baseLen + i] = pick(fc[i]);
                return a;
            };
            datasets.push({ label: 'Banda inf.', data: arr(function (p) { return p.lowerBound; }, false), isForecast: true, borderColor: 'transparent', backgroundColor: 'transparent', pointRadius: 0, borderWidth: 0, yAxisID: 'y', order: 0, spanGaps: true });
            datasets.push({ label: 'Banda sup.', data: arr(function (p) { return p.upperBound; }, false), isForecast: true, borderColor: 'transparent', backgroundColor: hexToRgba(FORE, 0.12), fill: '-1', pointRadius: 0, borderWidth: 0, yAxisID: 'y', order: 0, spanGaps: true });
            datasets.push({ label: 'Pronóstico', data: arr(function (p) { return p.value; }, true), isForecast: true, isForecastLine: true, borderColor: FORE, borderDash: [5, 4], backgroundColor: 'transparent', pointRadius: 0, borderWidth: 2, yAxisID: 'y', order: 0, spanGaps: true });
        }

        // El eje de volumen se escala para que las barras ocupen ~el 25% inferior del área.
        const volMax = maxVol > 0 ? maxVol * 4 : 1;

        // Techo (máximo) y suelo (mínimo) del precio en el rango; el eje Y se fija con un
        // margen relativo al precio (selector HV-040) para que la línea no quede pegada a los
        // bordes. 0 = ajustado (máxima extensión); mayor = más compresión.
        let hi = -Infinity, lo = Infinity;
        if (candle && ohlc) {
            // En velas el techo/suelo son el máximo de máximos y el mínimo de mínimos.
            ohlc.forEach(function (d) {
                if (!d) return;
                if (d.h > hi) hi = d.h;
                if (d.l < lo) lo = d.l;
            });
        } else {
            priceDatasets.forEach(function (ds) {
                ds.data.forEach(function (v) {
                    if (v === null || v === undefined) return;
                    const n = Number(v);
                    if (!Number.isFinite(n)) return;
                    if (n > hi) hi = n;
                    if (n < lo) lo = n;
                });
            });
        }
        if (fc) {   // que la banda de pronóstico quepa en el eje (HV-043)
            fc.forEach(function (p) {
                if (p.upperBound > hi) hi = p.upperBound;
                if (p.lowerBound < lo) lo = p.lowerBound;
            });
        }
        const hasHiLo = Number.isFinite(hi) && Number.isFinite(lo);
        let yMin, yMax;
        if (hasHiLo) {
            const margin = ((hi + lo) / 2) * yMarginPct;   // margen simétrico relativo al precio medio
            yMin = lo - margin;
            yMax = hi + margin;
            if (yMax - yMin < 1e-9) {   // datos planos + margen 0: evita un eje de rango nulo
                const eps = Math.max(Math.abs(hi) * 0.005, 0.5);
                yMin = lo - eps; yMax = hi + eps;
            }
        }

        if (!win.chart) {
            const ctx = win.els.canvas.getContext('2d');
            win.chart = new Chart(ctx, {
                type: 'line',
                data: { labels: labels, datasets: datasets },
                plugins: [candlePlugin, highLowLinesPlugin, currentValuePlugin],
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    animation: false,
                    interaction: { mode: 'nearest', intersect: false },
                    scales: {
                        x: {
                            type: 'category',
                            grid: { display: true, color: function () { return gridColor(); } },
                            ticks: { maxRotation: 0, autoSkip: true, autoSkipPadding: 14, color: function () { return axisTextColor(); } }
                        },
                        y: {
                            type: 'linear',
                            position: 'right',
                            min: yMin,
                            max: yMax,
                            grid: { display: true, color: function () { return gridColor(); } },
                            ticks: { callback: function (v) { return priceFmt(v); }, color: function () { return axisTextColor(); } }
                        },
                        yVol: {
                            type: 'linear',
                            position: 'left',
                            display: false,          // eje oculto: las barras solo aportan contexto de volumen
                            min: 0,
                            max: volMax,
                            grid: { display: false }
                        }
                    },
                    plugins: {
                        legend: {
                            position: 'top',
                            labels: {
                                filter: function (item, data) {
                                    const ds = data.datasets[item.datasetIndex];
                                    // ocultar volumen y las bandas de pronóstico; mostrar la línea "Pronóstico"
                                    return !(ds && (ds.isVolume || (ds.isForecast && !ds.isForecastLine)));
                                }
                            }
                        },
                        tooltip: {
                            callbacks: {
                                title: function (items) {
                                    if (!items.length) return '';
                                    const arr = items[0].chart.$labelsFull || [];
                                    return arr[items[0].dataIndex] || '';
                                },
                                label: function (ctx) {
                                    if (ctx.dataset.isVolume) return 'Volumen: ' + volFmt(ctx.parsed.y);
                                    const oc = ctx.chart.$ohlc;
                                    if (ctx.chart.$candleMode && oc && oc[ctx.dataIndex]) {
                                        const d = oc[ctx.dataIndex];
                                        return [ctx.dataset.label,
                                            'Apert.: ' + priceFmt(d.o), 'Máx: ' + priceFmt(d.h),
                                            'Mín: ' + priceFmt(d.l), 'Cierre: ' + priceFmt(d.c)];
                                    }
                                    return ctx.dataset.label + ': ' + priceFmt(ctx.parsed.y);
                                }
                            }
                        }
                    }
                }
            });
            // Los plugins (velas, techo/suelo, etiqueta de valor) leen estas props; hay que
            // fijarlas antes de dibujar. Tras crear, un update() las aplica al primer render.
            win.chart.$labelsFull = labelsFull;
            win.chart.$hiLo = hasHiLo ? { high: hi, low: lo } : null;
            win.chart.$candleMode = candle;
            win.chart.$ohlc = ohlc;
            win.chart.update();
        } else {
            win.chart.data.labels = labels;
            win.chart.data.datasets = datasets;
            if (win.chart.options.scales.yVol) win.chart.options.scales.yVol.max = volMax;
            win.chart.options.scales.y.min = yMin;
            win.chart.options.scales.y.max = yMax;
            win.chart.$labelsFull = labelsFull;   // título del tooltip (fecha/hora completa)
            win.chart.$hiLo = hasHiLo ? { high: hi, low: lo } : null;   // techo/suelo (HV-039)
            win.chart.$candleMode = candle;       // velas japonesas (HV-041)
            win.chart.$ohlc = ohlc;
            win.chart.update();
        }
    }

    // ── Evolución del valor de cuenta (HV-017) ─────────────────────────────
    let accountChart = null;
    let lastAccountHistory = [];       // snapshots crudos (para re-filtrar por rango sin refetch)
    let accountRange = 'ALL';          // rango temporal: 1D | 1W | 1M | ALL (HV-026)
    const ACCOUNT_RANGE_ALLOWED = ['1D', '1W', '1M', 'ALL'];
    const ACCOUNT_RANGE_DAYS = { '1D': 1, '1W': 7, '1M': 30 };
    const ACCOUNT_RANGE_LABEL = { '1D': '1 día', '1W': '1 semana', '1M': '1 mes', 'ALL': 'todo' };
    // Snapshots normales cada ~5 min; un salto mayor a esto es un hueco entre sesiones
    // (app apagada) → cortar la línea para no comprimir el tramo reciente en un "pico" (HV-025).
    const ACCOUNT_GAP_MS = 30 * 60 * 1000;

    // Filtra los snapshots a la ventana temporal seleccionada, anclada al último snapshot
    // disponible (no a la hora actual), para que la vista reciente siempre tenga datos.
    function filterAccountByRange(points, range) {
        if (range === 'ALL' || !points || points.length === 0) return points || [];
        const days = ACCOUNT_RANGE_DAYS[range];
        if (!days) return points;
        const lastTs = new Date(points[points.length - 1].timestamp).getTime();
        const cutoff = lastTs - days * 86400000;
        return points.filter(function (p) { return new Date(p.timestamp).getTime() >= cutoff; });
    }

    function renderAccountChart(points) {
        if (typeof Chart === 'undefined') return;
        const data = (points || [])
            .map(function (p) { return { x: new Date(p.timestamp).getTime(), av: Number(p.accountValue), nd: Number(p.netDeposits) }; })
            .filter(function (p) { return Number.isFinite(p.x) && Number.isFinite(p.av); });

        const hint = document.getElementById('accountHistoryHint');
        if (data.length === 0) {
            if (hint) hint.textContent = 'Aún no hay snapshots. Se toma uno al arrancar y luego cada pocos minutos.';
            if (accountChart) { accountChart.destroy(); accountChart = null; }
            return;
        }
        if (hint) {
            const last = points[points.length - 1];
            hint.textContent = data.length + ' snapshot(s) · rango ' + (ACCOUNT_RANGE_LABEL[accountRange] || 'todo')
                + ' · último valor ' + eur(last.accountValue) + ' (' + pctSigned(last.returnPct) + ')';
        }

        const accountData = insertGaps(data.map(function (p) { return { x: p.x, y: p.av }; }), ACCOUNT_GAP_MS);
        const depositsData = insertGaps(data.map(function (p) { return { x: p.x, y: p.nd }; }), ACCOUNT_GAP_MS);
        const datasets = [
            {
                label: 'Valor de cuenta', data: accountData, spanGaps: false,
                borderColor: '#198754', backgroundColor: 'rgba(25,135,84,0.10)',
                fill: true, tension: 0.15, pointRadius: 0, borderWidth: 2
            },
            {
                label: 'Aportado neto', data: depositsData, spanGaps: false,
                borderColor: '#6c757d', backgroundColor: 'transparent',
                borderDash: [5, 4], tension: 0, pointRadius: 0, borderWidth: 1.5
            }
        ];

        const xb = computeXBounds(datasets);
        const unit = timeUnitFor(xb);

        if (accountChart === null) {
            const el = document.getElementById('chartAccount');
            if (!el) return;
            accountChart = new Chart(el.getContext('2d'), {
                type: 'line',
                data: { datasets: datasets },
                options: {
                    responsive: true, maintainAspectRatio: false, animation: false,
                    interaction: { mode: 'index', intersect: false },
                    scales: {
                        x: {
                            type: 'time', time: { unit: unit, displayFormats: DISPLAY_FORMATS },
                            grid: { display: true, color: function () { return gridColor(); } },
                            ticks: { maxRotation: 0, autoSkip: true, color: function () { return axisTextColor(); } },
                            min: xb ? xb.min : undefined, max: xb ? xb.max : undefined
                        },
                        y: {
                            grid: { display: true, color: function () { return gridColor(); } },
                            ticks: { callback: function (v) { return eur(v); }, color: function () { return axisTextColor(); } }
                        }
                    },
                    plugins: {
                        legend: { position: 'top' },
                        tooltip: { callbacks: { label: function (ctx) { return ctx.dataset.label + ': ' + eur(ctx.parsed.y); } } }
                    }
                }
            });
        } else {
            accountChart.data.datasets = datasets;
            accountChart.options.scales.x.time.unit = unit;
            if (xb) { accountChart.options.scales.x.min = xb.min; accountChart.options.scales.x.max = xb.max; }
            accountChart.update();
        }
    }

    // ── Métricas avanzadas de cartera (HV-048): drawdown, Sharpe, volatilidad, profit factor ──
    async function fetchMetrics() {
        try {
            const r = await fetch('/api/account/metrics');
            if (!r.ok) return;
            const m = await r.json();
            const set = function (id, text, cls) {
                const el = document.getElementById(id);
                if (!el) return;
                el.textContent = text;
                // 'js-sensitive' viene del template (HV-051, modo privacidad) y hay que
                // reponerlo: reasignar className entero lo borraría en cada refresco.
                el.className = 'fs-4 fw-semibold js-sensitive ' + (cls || 'text-body');
            };
            const enoughDd = (m.snapshotCount || 0) >= 2;
            set('mMaxDd', enoughDd ? pctSigned(m.maxDrawdownPct) : '—', enoughDd && m.maxDrawdownPct < 0 ? 'text-danger' : 'text-body');
            const curEl = document.getElementById('mCurDd');
            if (curEl) curEl.textContent = enoughDd ? pctSigned(m.currentDrawdownPct) : '—';
            set('mSharpe', m.hasEnoughData ? Number(m.sharpeRatio).toFixed(2) : '—', m.hasEnoughData ? signClass(m.sharpeRatio) : 'text-muted');
            set('mVol', m.hasEnoughData ? Number(m.annualizedVolatilityPct).toFixed(2) + '%' : '—', 'text-body');
            if (m.profitFactorInfinite) set('mPf', '∞', 'text-success');
            else if (m.profitFactor > 0) set('mPf', Number(m.profitFactor).toFixed(2), m.profitFactor >= 1 ? 'text-success' : 'text-danger');
            else set('mPf', '—', 'text-muted');
            const hint = document.getElementById('metricsHint');
            if (hint) hint.textContent = m.message ? ('· ' + m.message) : '· Sharpe/volatilidad sobre retornos diarios (√252), aproximados con pocos datos';
        } catch (e) { /* silencioso */ }
    }

    // ── Resultados por periodo: trades cerrados por año/mes (HV-049) ──────────────
    const MONTHS_ES = ['ene', 'feb', 'mar', 'abr', 'may', 'jun', 'jul', 'ago', 'sep', 'oct', 'nov', 'dic'];
    async function fetchBreakdown() {
        const body = document.getElementById('breakdownBody');
        const totalEl = document.getElementById('breakdownTotal');
        if (!body) return;
        try {
            const r = await fetch('/api/account/closed-breakdown');
            if (!r.ok) return;
            const d = await r.json();
            const cur = d.baseCurrency || baseCurrency;
            if (totalEl) {
                totalEl.textContent = 'Total: ' + money(d.totalPnLBase, cur) + ' · ' + d.totalTrades + ' trades';
                // 'js-sensitive' (HV-051): reponerlo, reasignar className lo borraría cada refresco.
                totalEl.className = 'small fw-semibold js-sensitive ' + signClass(d.totalPnLBase);
            }
            if (!d.years || d.years.length === 0) {
                body.innerHTML = '<tr><td colspan="4" class="text-center text-muted">Sin trades cerrados</td></tr>';
                return;
            }
            const wl = function (w, l) { return '<span class="text-success">' + w + '</span> / <span class="text-danger">' + l + '</span>'; };
            let html = '';
            d.years.forEach(function (y) {
                // Fila de año (resumen, en negrita).
                html += '<tr class="table-light fw-semibold">'
                    + '<td>' + y.year + '</td>'
                    + '<td class="text-end ' + signClass(y.pnLBase) + '">' + money(y.pnLBase, cur) + '</td>'
                    + '<td class="text-end">' + y.trades + '</td>'
                    + '<td class="text-end">' + wl(y.wins, y.losses) + '</td>'
                    + '</tr>';
                (y.months || []).forEach(function (m) {
                    html += '<tr>'
                        + '<td class="ps-4 text-muted">' + (MONTHS_ES[m.month - 1] || m.month) + '</td>'
                        + '<td class="text-end ' + signClass(m.pnLBase) + '">' + money(m.pnLBase, cur) + '</td>'
                        + '<td class="text-end">' + m.trades + '</td>'
                        + '<td class="text-end">' + wl(m.wins, m.losses) + '</td>'
                        + '</tr>';
                });
            });
            body.innerHTML = html;
        } catch (e) { /* silencioso */ }
    }

    async function fetchAccountHistory() {
        try {
            const resp = await fetch('/api/account/history?points=5000');
            if (!resp.ok) { console.warn('account history fetch failed:', resp.status); return; }
            lastAccountHistory = await resp.json();
            renderAccountChart(filterAccountByRange(lastAccountHistory, accountRange));
        } catch (err) {
            console.error('account history fetch error', err);
        }
    }

    function initAccountHistoryControl() {
        try {
            const saved = localStorage.getItem('accountRange');
            if (ACCOUNT_RANGE_ALLOWED.indexOf(saved) !== -1) accountRange = saved;
        } catch (e) { }
        const sel = document.getElementById('accountRangeSelect');
        if (sel) {
            sel.value = accountRange;
            sel.addEventListener('change', function () {
                accountRange = ACCOUNT_RANGE_ALLOWED.indexOf(sel.value) !== -1 ? sel.value : 'ALL';
                try { localStorage.setItem('accountRange', accountRange); } catch (e) { }
                // Re-filtrar desde la caché (sin volver a pedir); si no hay caché, pedir.
                if (lastAccountHistory.length) renderAccountChart(filterAccountByRange(lastAccountHistory, accountRange));
                else fetchAccountHistory();
            });
        }
    }

    // Métricas/estado/tablas siempre en vivo. Las ventanas de precios (HV-054) ya no
    // dependen de este ciclo: cada una pinta histórico y se recarga por su cuenta.
    const METRICS_MS = 3000;
    const ALLOWED_MS = [3000, 30000, 60000, 120000, 300000];
    let chartIntervalMs = REFRESH_MS;
    let metricsTimer = null;

    async function fetchAndRender() {
        try {
            const response = await fetch('/api/dashboard/data?points=' + historyPoints);
            if (!response.ok) {
                console.warn('Dashboard fetch failed:', response.status);
                return;
            }
            const data = await response.json();
            lastSeries = data.priceSeries || [];
            ensureSymbolOptions(lastSeries); // opciones de símbolo desde los datos en vivo
            // Métricas/estado/tablas: siempre en vivo.
            renderStatus(data);
            renderMetrics(data);
            renderOpenTrades(data.openTrades);
            renderClosedTrades(data.recentClosedTrades);
            // Las ventanas de precios (HV-054) se pintan con histórico real de Yahoo y tienen
            // su propio ciclo de refresco: este polling de 3 s no las toca (abrir N ventanas
            // no multiplica por N las peticiones al proveedor).
        } catch (err) {
            console.error('Dashboard fetch error', err);
        }
    }

    // ── Control de frecuencia de refresco del GRÁFICO ───────────────────────
    function initChartRefreshControl() {
        try {
            const saved = parseInt(localStorage.getItem('chartRefreshMs'), 10);
            if (ALLOWED_MS.indexOf(saved) !== -1) chartIntervalMs = saved;
        } catch (e) { }

        const sel = document.getElementById('refreshSelect');
        if (sel) {
            sel.value = String(chartIntervalMs);
            sel.addEventListener('change', function () {
                const next = parseInt(sel.value, 10);
                chartIntervalMs = ALLOWED_MS.indexOf(next) !== -1 ? next : REFRESH_MS;
                try { localStorage.setItem('chartRefreshMs', String(chartIntervalMs)); } catch (e) { }
                fetchAndRender(true); // aplicar de inmediato (redibuja el gráfico)
            });
        }
    }

    // ── Selector de histórico (nº de puntos) ────────────────────────────────
    function initHistoryControl() {
        try {
            const saved = parseInt(localStorage.getItem('historyPoints'), 10);
            if (HISTORY_ALLOWED.indexOf(saved) !== -1) historyPoints = saved;
        } catch (e) { }

        const sel = document.getElementById('historySelect');
        if (sel) {
            sel.value = String(historyPoints);
            sel.addEventListener('change', function () {
                const next = parseInt(sel.value, 10);
                historyPoints = HISTORY_ALLOWED.indexOf(next) !== -1 ? next : 50;
                try { localStorage.setItem('historyPoints', String(historyPoints)); } catch (e) { }
                fetchAndRender(true); // re-pedir con el nuevo histórico y redibujar
            });
        }
    }

    // ── Señales ML.NET por ventana (HV-043/044, adaptado a multi-ventana HV-054) ──
    function renderForecastBadge(win) {
        const el = win.els.forecastBadge;
        if (!el) return;
        if (!win.forecastActive) { el.textContent = ''; el.className = 'js-win-forecast-badge small fw-semibold'; return; }
        const f = win.forecast;
        if (!f || f.symbol !== win.symbol || f.range !== win.range) {
            el.textContent = '🔮 …'; el.className = 'js-win-forecast-badge small fw-semibold text-muted'; return;
        }
        if (!f.hasForecast) {
            el.textContent = '🔮 ' + (f.message || 'sin pronóstico');
            el.className = 'js-win-forecast-badge small fw-semibold text-muted';
            return;
        }
        const cls = f.signal === 'Alcista' ? 'text-success' : f.signal === 'Bajista' ? 'text-danger' : 'text-secondary';
        el.textContent = '🔮 ' + win.range + ': ' + f.signal + ' ' + pctSigned(f.expectedChangePct) + ' (' + f.points.length + 'p)';
        el.className = 'js-win-forecast-badge small fw-semibold ' + cls;
    }

    async function loadForecast(win) {
        if (!win.forecastActive) { win.forecast = null; renderForecastBadge(win); return; }
        renderForecastBadge(win);   // muestra "…" mientras llega
        const sym = win.symbol, rng = win.range;
        try {
            const resp = await fetch('/api/forecast?symbol=' + encodeURIComponent(sym)
                + '&range=' + encodeURIComponent(rng) + '&horizon=' + FORECAST_HORIZON);
            if (!resp.ok) { win.forecast = null; renderForecastBadge(win); return; }
            const f = await resp.json();
            // Ignora respuestas obsoletas (cambió el rango, o se cerró la ventana, entretanto).
            if (win.closed || f.symbol !== win.symbol || f.range !== win.range) return;
            win.forecast = f;
            renderForecastBadge(win);
            if (win.lastSeries) renderChart(win, win.lastSeries);   // pinta el overlay
        } catch (e) {
            win.forecast = null; renderForecastBadge(win);
        }
    }

    // Clasificación sube/baja (HV-044): badge con dirección/señal + calidad del modelo.
    function renderDirectionBadge(win) {
        const el = win.els.signalBadge;
        if (!el) return;
        if (!win.forecastActive) { el.textContent = ''; el.className = 'js-win-signal-badge small fw-semibold'; el.title = ''; return; }
        const s = win.signal;
        if (!s || s.symbol !== win.symbol || s.range !== win.range) {
            el.textContent = '📊 …'; el.className = 'js-win-signal-badge small fw-semibold text-muted'; el.title = ''; return;
        }
        if (!s.hasPrediction) {
            el.textContent = '📊 ' + (s.message || 'sin señal');
            el.className = 'js-win-signal-badge small fw-semibold text-muted'; el.title = '';
            return;
        }
        const cls = s.signal === 'Comprar' ? 'text-success' : s.signal === 'Vender' ? 'text-danger' : 'text-secondary';
        const pUp = Math.round((s.probability || 0) * 100);
        el.textContent = '📊 ' + s.signal + ' (' + s.direction + ' ' + pUp + '%)';
        el.className = 'js-win-signal-badge small fw-semibold ' + cls;
        // Honestidad: modelo elegido + calidad (hold-out) en el tooltip.
        el.title = 'Modelo ' + (s.modelUsed || '-') + ' · ' + (s.featureCount || 0) + ' features técnicas · acierto '
            + Math.round((s.accuracy || 0) * 100) + '% / AUC ' + (s.auc || 0).toFixed(2)
            + ' en hold-out (n=' + s.trainSamples + '). Indicador, no asesoramiento.';
    }

    async function loadSignal(win) {
        if (!win.forecastActive) { win.signal = null; renderDirectionBadge(win); return; }
        renderDirectionBadge(win);
        try {
            const resp = await fetch('/api/signal?symbol=' + encodeURIComponent(win.symbol)
                + '&range=' + encodeURIComponent(win.range));
            if (!resp.ok) { win.signal = null; renderDirectionBadge(win); return; }
            const s = await resp.json();
            if (win.closed || s.symbol !== win.symbol || s.range !== win.range) return;   // obsoleta
            win.signal = s;
            renderDirectionBadge(win);
        } catch (e) {
            win.signal = null; renderDirectionBadge(win);
        }
    }

    // ── Noticias por símbolo (HV-055) ───────────────────────────────────────
    // A diferencia de pronóstico/señal, las noticias son por símbolo (no por rango): se piden
    // una vez al activar el switch y no se recargan al cambiar de rango.
    function renderNewsList(win) {
        const el = win.els.newsList;
        if (!el) return;
        if (!win.newsActive) { el.classList.add('d-none'); el.innerHTML = ''; return; }
        el.classList.remove('d-none');

        const n = win.news;
        if (!n) { el.innerHTML = '<div class="text-muted">Cargando noticias…</div>'; return; }
        if (!n.hasNews || !n.items || !n.items.length) {
            el.innerHTML = '<div class="text-muted">' + escapeHtml(n.message || 'Sin noticias.') + '</div>';
            return;
        }

        el.innerHTML = n.items.map(function (item) {
            const when = item.publishedAtUtc ? new Date(item.publishedAtUtc).toLocaleString('es-ES') : '';
            const pub = item.publisher ? escapeHtml(item.publisher) : '';
            // Fuente del titular (HV-057): Yahoo/onvista combinados en una sola lista por fecha.
            const source = item.source ? escapeHtml(item.source) : '';
            const badge = source ? '<span class="badge news-source-badge news-source-' + source.toLowerCase() + '">' + source + '</span>' : '';
            const meta = [pub, when].filter(Boolean).join(' · ');
            return '<div class="news-item">'
                + badge
                + '<a href="' + escapeHtml(item.link) + '" target="_blank" rel="noopener noreferrer">'
                +   escapeHtml(item.title) + '</a>'
                + (meta ? '<div class="text-muted news-item-meta">' + meta + '</div>' : '')
                + '</div>';
        }).join('');
    }

    async function loadNews(win) {
        if (!win.newsActive) { win.news = null; renderNewsList(win); return; }
        renderNewsList(win);   // muestra "Cargando…" mientras llega
        try {
            const resp = await fetch('/api/news?symbol=' + encodeURIComponent(win.symbol) + '&max=6');
            if (!resp.ok) { win.news = null; renderNewsList(win); return; }
            const n = await resp.json();
            if (win.closed || n.symbol !== win.symbol) return;   // la ventana se cerró entretanto
            win.news = n;
            renderNewsList(win);
        } catch (e) {
            win.news = null; renderNewsList(win);
        }
    }

    // ── Histórico real de Yahoo (/api/history) para una ventana ────────────────
    async function loadHistory(win) {
        const hint = win.els.rangeHint;
        if (hint) hint.textContent = 'Cargando histórico ' + win.range + '…';
        const range = win.range;
        try {
            const resp = await fetch('/api/history?symbol=' + encodeURIComponent(win.symbol)
                + '&range=' + encodeURIComponent(range));
            const s = resp.ok ? await resp.json() : null;
            // La ventana pudo cerrarse o cambiar de rango mientras llegaba la respuesta.
            if (win.closed || win.range !== range) return;
            if (!s || !s.points || !s.points.length) {
                if (hint) hint.textContent = 'Sin datos de histórico para ' + range + '.';
                return;
            }
            renderChart(win, [s]);
            if (win.forecastActive) { loadForecast(win); loadSignal(win); }   // dependen de símbolo+rango
            if (hint) hint.textContent = 'Histórico · ' + range;
        } catch (e) {
            console.error('Error cargando histórico', e);
            if (win.closed) return;
            if (hint) hint.textContent = 'Error cargando histórico.';
        }
    }

    // Recarga el histórico de todas las ventanas abiertas de forma SECUENCIAL: en paralelo,
    // N ventanas dispararían N peticiones simultáneas al proveedor (Yahoo/Twelve Data tienen
    // límites de tasa: el plan free de TD son 8 req/min).
    async function reloadAllPriceWindows() {
        const wins = priceWindows.slice();   // copia: una ventana puede cerrarse durante el bucle
        for (const w of wins) {
            if (w.closed) continue;
            await loadHistory(w);
        }
    }

    // ── Ventanas de precios: creación, cierre y persistencia (HV-054) ──────────
    function savePriceWindows() {
        try {
            localStorage.setItem(PRICE_WINDOWS_KEY, JSON.stringify(priceWindows.map(function (w) {
                return {
                    symbol: w.symbol, range: w.range, chartType: w.chartType,
                    yMarginPct: w.yMarginPct, forecastActive: w.forecastActive,
                    newsActive: w.newsActive
                };
            })));
        } catch (e) { }
    }

    function closePriceWindow(win) {
        const i = priceWindows.indexOf(win);
        if (i === -1) return;
        win.closed = true;                       // corta los fetch en vuelo de esta ventana
        priceWindows.splice(i, 1);
        if (win.chart) { win.chart.destroy(); win.chart = null; }   // libera la instancia de Chart.js
        if (win.els.card && win.els.card.parentNode) win.els.card.parentNode.removeChild(win.els.card);
        savePriceWindows();
        ensureSymbolOptions(lastSeries);         // el símbolo vuelve a estar disponible en el combo
    }

    function priceWindowMarkup(win) {
        const sym = escapeHtml(win.symbol);
        const rangeBtns = RANGES.map(function (r) {
            return '<button type="button" class="btn btn-outline-secondary' + (r === win.range ? ' active' : '')
                + '" data-range="' + r + '">' + r + '</button>';
        }).join('');
        const marginOpts = [
            [0, 'Ajustado'], [0.01, '±1 %'], [0.02, '±2 %'],
            [0.05, '±5 %'], [0.10, '±10 %'], [0.20, '±20 %']
        ].map(function (o) {
            return '<option value="' + o[0] + '"' + (o[0] === win.yMarginPct ? ' selected' : '') + '>' + o[1] + '</option>';
        }).join('');

        return '<div class="card-body">'
            + '<div class="d-flex flex-wrap align-items-center gap-2 mb-2">'
            +   '<h6 class="card-title mb-0 me-auto">' + sym
            +     ' <small class="text-muted fw-normal">(valor, eje a la derecha)</small></h6>'
            +   '<span class="js-win-delta small fw-semibold" title="Variación acumulada en el rango seleccionado"></span>'
            +   '<button type="button" class="btn btn-outline-danger btn-sm py-0 js-win-close" '
            +     'title="Cerrar esta ventana (el símbolo se sigue vigilando)" '
            +     'aria-label="Cerrar ventana de ' + sym + '">✕</button>'
            + '</div>'
            + '<div class="d-flex flex-wrap align-items-center gap-2 mb-2">'
            +   '<div class="form-check form-switch mb-0 small" title="Línea o velas japonesas (OHLC)">'
            +     '<input class="form-check-input js-win-candle" type="checkbox" role="switch" id="' + win.id + '-candle"'
            +       (win.chartType === 'candle' ? ' checked' : '') + '>'
            +     '<label class="form-check-label text-muted" for="' + win.id + '-candle">🕯️ Velas</label>'
            +   '</div>'
            +   '<div class="form-check form-switch mb-0 small" title="Señales ML.NET: pronóstico SSA y clasificación sube/baja. Indicadores, no asesoramiento financiero">'
            +     '<input class="form-check-input js-win-forecast" type="checkbox" role="switch" id="' + win.id + '-fc"'
            +       (win.forecastActive ? ' checked' : '') + '>'
            +     '<label class="form-check-label text-muted" for="' + win.id + '-fc">🔮 Señales ML</label>'
            +   '</div>'
            +   '<span class="js-win-forecast-badge small fw-semibold"></span>'
            +   '<span class="js-win-signal-badge small fw-semibold"></span>'
            +   '<div class="form-check form-switch mb-0 small" title="Titulares recientes de Yahoo Finance para este símbolo">'
            +     '<input class="form-check-input js-win-news" type="checkbox" role="switch" id="' + win.id + '-news"'
            +       (win.newsActive ? ' checked' : '') + '>'
            +     '<label class="form-check-label text-muted" for="' + win.id + '-news">📰 Noticias</label>'
            +   '</div>'
            + '</div>'
            + '<div class="chart-wrap" style="height: 320px;"><canvas class="js-win-canvas"></canvas></div>'
            + '<div class="d-flex flex-wrap align-items-center gap-2 mt-2">'
            +   '<div class="btn-group btn-group-sm js-win-range" role="group" aria-label="Rango temporal">' + rangeBtns + '</div>'
            +   '<span class="js-win-hint text-muted small"></span>'
            +   '<label class="d-flex align-items-center gap-1 mb-0 small ms-auto" title="Margen del eje Y: menos margen = línea más ampliada">'
            +     '<span class="text-muted">Margen Y:</span>'
            +     '<select class="js-win-margin form-select form-select-sm w-auto py-0">' + marginOpts + '</select>'
            +   '</label>'
            + '</div>'
            + '<div class="js-win-news-list news-list small mt-2' + (win.newsActive ? '' : ' d-none') + '"></div>'
            + '</div>';
    }

    function createPriceWindow(symbol, opts) {
        const container = document.getElementById('priceWindowsContainer');
        if (!container || !symbol) return null;
        // Una sola ventana por símbolo: si ya existe, la resaltamos en vez de duplicarla.
        const existing = priceWindows.find(function (w) { return w.symbol === symbol; });
        if (existing) {
            existing.els.card.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
            return existing;
        }
        if (priceWindows.length >= MAX_PRICE_WINDOWS) {
            alert('Máximo ' + MAX_PRICE_WINDOWS + ' ventanas abiertas. Cierra alguna para abrir otra.');
            return null;
        }

        const o = opts || {};
        const win = {
            id: 'pw' + (++winSeq),
            symbol: symbol,
            colorIdx: winSeq - 1,
            range: RANGES.indexOf(o.range) !== -1 ? o.range : '1D',
            chartType: o.chartType === 'candle' ? 'candle' : 'line',
            yMarginPct: Y_MARGIN_ALLOWED.indexOf(o.yMarginPct) !== -1 ? o.yMarginPct : 0.02,
            forecastActive: !!o.forecastActive,
            forecast: null,
            signal: null,
            newsActive: !!o.newsActive,
            news: null,
            chart: null,
            lastSeries: null,
            closed: false,
            els: {}
        };

        const card = document.createElement('div');
        card.className = 'card shadow-sm price-window';
        card.id = win.id;
        card.innerHTML = priceWindowMarkup(win);
        container.appendChild(card);

        win.els = {
            card: card,
            canvas: card.querySelector('.js-win-canvas'),
            delta: card.querySelector('.js-win-delta'),
            rangeHint: card.querySelector('.js-win-hint'),
            forecastBadge: card.querySelector('.js-win-forecast-badge'),
            signalBadge: card.querySelector('.js-win-signal-badge'),
            rangeBar: card.querySelector('.js-win-range'),
            newsList: card.querySelector('.js-win-news-list')
        };

        // ── Listeners propios de esta ventana ──
        card.querySelector('.js-win-close').addEventListener('click', function () {
            closePriceWindow(win);
        });

        win.els.rangeBar.addEventListener('click', function (e) {
            const btn = e.target.closest('button[data-range]');
            if (!btn) return;
            Array.prototype.forEach.call(win.els.rangeBar.querySelectorAll('button'),
                function (b) { b.classList.remove('active'); });
            btn.classList.add('active');
            win.range = btn.getAttribute('data-range');
            win.forecast = null; win.signal = null;   // las señales son por símbolo+rango
            savePriceWindows();
            loadHistory(win);
        });

        card.querySelector('.js-win-margin').addEventListener('change', function (e) {
            const next = parseFloat(e.target.value);
            win.yMarginPct = Y_MARGIN_ALLOWED.indexOf(next) !== -1 ? next : 0.02;
            savePriceWindows();
            if (win.lastSeries) renderChart(win, win.lastSeries);   // recalcula el eje sin refetch
        });

        card.querySelector('.js-win-candle').addEventListener('change', function (e) {
            win.chartType = e.target.checked ? 'candle' : 'line';
            savePriceWindows();
            if (win.lastSeries) renderChart(win, win.lastSeries);   // re-render sin refetch
        });

        card.querySelector('.js-win-forecast').addEventListener('change', function (e) {
            win.forecastActive = e.target.checked;
            savePriceWindows();
            if (win.forecastActive) {
                loadForecast(win);
                loadSignal(win);
            } else {
                win.forecast = null;
                win.signal = null;
                renderForecastBadge(win);
                renderDirectionBadge(win);
                if (win.lastSeries) renderChart(win, win.lastSeries);   // quita el overlay
            }
        });

        card.querySelector('.js-win-news').addEventListener('change', function (e) {
            win.newsActive = e.target.checked;
            savePriceWindows();
            if (win.newsActive) {
                loadNews(win);
            } else {
                win.news = null;
                renderNewsList(win);
            }
        });

        priceWindows.push(win);
        savePriceWindows();
        ensureSymbolOptions(lastSeries);   // el símbolo ya no está libre
        // deferLoad: al restaurar varias ventanas de golpe, el llamante las carga en serie
        // (reloadAllPriceWindows) en vez de lanzar N peticiones simultáneas al proveedor.
        if (!o.deferLoad) loadHistory(win);
        // Las noticias son por símbolo (no dependen del rango): se cargan aparte del histórico.
        if (win.newsActive) loadNews(win);
        return win;
    }

    // Preferencias del gráfico único anterior a HV-054 (chartRange/chartType/…): se usan como
    // valores por defecto de la primera ventana para que el panel no "pierda" los ajustes del
    // usuario al estrenar el multi-ventana. Si no hay nada guardado, quedan los defaults.
    function legacyChartPrefs() {
        const p = {};
        try {
            const r = localStorage.getItem('chartRange');
            if (RANGES.indexOf(r) !== -1) p.range = r;
            if (localStorage.getItem('chartType') === 'candle') p.chartType = 'candle';
            const m = parseFloat(localStorage.getItem('chartYMargin'));
            if (Y_MARGIN_ALLOWED.indexOf(m) !== -1) p.yMarginPct = m;
            if (localStorage.getItem('chartForecast') === '1') p.forecastActive = true;
        } catch (e) { }
        return p;
    }

    function initPriceWindows() {
        const sel = document.getElementById('newPriceWindowSelect');
        if (sel) {
            sel.addEventListener('change', function () {
                const sym = sel.value;
                sel.value = '';
                if (sym) createPriceWindow(sym);
            });
        }

        // Restaura las ventanas de la sesión anterior (con su rango/velas/margen/señales).
        let saved = [];
        try {
            const raw = localStorage.getItem(PRICE_WINDOWS_KEY);
            if (raw) saved = JSON.parse(raw) || [];
        } catch (e) { saved = []; }
        return saved;
    }

    // ── Buscador de instrumentos (HV-010) + alta en el panel (HV-011) ──────────
    function escapeHtml(s) {
        return String(s == null ? '' : s)
            .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;').replace(/'/g, '&#39;');
    }

    function renderSearchResults(items) {
        const wrap = document.getElementById('searchResultsWrap');
        const body = document.getElementById('searchResultsBody');
        if (!wrap || !body) return;
        if (!items || items.length === 0) {
            body.innerHTML = '<tr><td colspan="5" class="text-center text-muted">Sin resultados.</td></tr>';
            wrap.style.display = '';
            return;
        }
        body.innerHTML = items.map(function (r) {
            const sym = escapeHtml(r.symbol);
            return '<tr>'
                + '<td><strong>' + sym + '</strong></td>'
                + '<td>' + escapeHtml(r.name) + '</td>'
                + '<td>' + escapeHtml(r.exchange) + '</td>'
                + '<td>' + escapeHtml(r.type) + '</td>'
                + '<td class="text-end"><button type="button" class="btn btn-sm btn-outline-primary" '
                + 'data-track="' + sym + '" data-name="' + escapeHtml(r.name) + '">+ Añadir</button></td>'
                + '</tr>';
        }).join('');
        wrap.style.display = '';
    }

    async function trackSymbol(symbol, name, btn) {
        if (btn) { btn.disabled = true; btn.textContent = 'Añadiendo…'; }
        try {
            const resp = await fetch('/api/instruments/track', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ symbol: symbol, name: name })
            });
            if (!resp.ok) throw new Error('HTTP ' + resp.status);
            // Cierra la modal del buscador tras añadir.
            const modalEl = document.getElementById('searchModal');
            if (modalEl && window.bootstrap) {
                window.bootstrap.Modal.getOrCreateInstance(modalEl).hide();
            }
            refreshTrackedCurrencies();
            // Abre directamente la ventana del símbolo recién añadido (HV-054).
            createPriceWindow(symbol.toUpperCase());
            fetchAndRender(true);
        } catch (e) {
            if (btn) { btn.disabled = false; btn.textContent = '+ Añadir'; }
            alert('No se pudo añadir el símbolo: ' + e.message);
        }
    }

    function initInstrumentSearch() {
        const form = document.getElementById('searchForm');
        const input = document.getElementById('searchInput');
        const hint = document.getElementById('searchHint');
        const body = document.getElementById('searchResultsBody');
        if (!form || !input) return;

        form.addEventListener('submit', async function (e) {
            e.preventDefault();
            const q = input.value.trim();
            if (q.length < 2) { if (hint) hint.textContent = 'Escribe al menos 2 caracteres.'; return; }
            if (hint) hint.textContent = 'Buscando…';
            try {
                const resp = await fetch('/api/instruments/search?q=' + encodeURIComponent(q));
                if (!resp.ok) throw new Error('HTTP ' + resp.status);
                const items = await resp.json();
                renderSearchResults(items);
                if (hint) hint.textContent = items.length + ' resultado(s).';
            } catch (e2) {
                if (hint) hint.textContent = 'Error en la búsqueda: ' + e2.message;
            }
        });

        if (body) {
            body.addEventListener('click', function (e) {
                const btn = e.target.closest('[data-track]');
                if (!btn) return;
                trackSymbol(btn.getAttribute('data-track'), btn.getAttribute('data-name') || '', btn);
            });
        }

        // Al abrir la modal: enfocar el campo y limpiar la búsqueda anterior.
        const modalEl = document.getElementById('searchModal');
        if (modalEl) {
            modalEl.addEventListener('shown.bs.modal', function () {
                input.focus();
                input.select();
            });
            modalEl.addEventListener('hidden.bs.modal', function () {
                input.value = '';
                const wrap = document.getElementById('searchResultsWrap');
                if (wrap) { wrap.style.display = 'none'; }
                if (body) { body.innerHTML = ''; }
                if (hint) { hint.textContent = 'Yahoo no indexa WKN; usa descripción, ISIN o ticker.'; }
            });
        }
    }


    // ── Posiciones reales del usuario (HV-013): acciones por fila + modal de alta ──
    function initPositionsActions() {
        const tbody = document.getElementById('openTradesBody');
        if (!tbody) return;
        tbody.addEventListener('click', async function (e) {
            const toggleRow = e.target.closest('[data-toggle-group]');
            if (toggleRow) {
                const sym = toggleRow.getAttribute('data-toggle-group');
                const expanded = toggleRow.getAttribute('data-expanded') === '1';
                // Fuente de verdad fuera del DOM: si no se recuerda aquí, el próximo refresco del
                // dashboard (polling) reconstruye la tabla ya colapsada de nuevo (bug reportado).
                if (expanded) expandedGroups.delete(sym); else expandedGroups.add(sym);
                toggleRow.setAttribute('data-expanded', expanded ? '0' : '1');
                const caret = toggleRow.querySelector('.js-lot-caret');
                if (caret) caret.textContent = expanded ? '▸' : '▾';
                tbody.querySelectorAll('tr.js-lot-detail[data-group="' + sym + '"]').forEach(function (row) {
                    row.style.display = expanded ? 'none' : '';
                });
                return;
            }
            const closeBtn = e.target.closest('[data-close]');
            const delBtn = e.target.closest('[data-del]');
            if (closeBtn) {
                const id = closeBtn.getAttribute('data-close');
                const sym = closeBtn.getAttribute('data-symbol') || '';
                const cur = closeBtn.getAttribute('data-price') || '';
                const input = prompt('Cerrar ' + sym + ' — precio de cierre (€):', cur);
                if (input === null) return;
                const exit = parseFloat(String(input).replace(',', '.'));
                if (!(exit > 0)) { alert('Precio de cierre inválido.'); return; }
                closeBtn.disabled = true;
                try {
                    const resp = await fetch('/api/positions/' + encodeURIComponent(id) + '/close', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ exitPrice: exit })
                    });
                    if (!resp.ok) throw new Error((await resp.text()) || ('HTTP ' + resp.status));
                    fetchAndRender(false);
                } catch (err) {
                    closeBtn.disabled = false;
                    alert('No se pudo cerrar la posición: ' + err.message);
                }
                return;
            }
            if (delBtn) {
                const id = delBtn.getAttribute('data-del');
                const sym = delBtn.getAttribute('data-symbol') || '';
                if (!confirm('¿Eliminar la posición de ' + sym + '? (la borra del seguimiento, no la cierra)')) return;
                delBtn.disabled = true;
                try {
                    const resp = await fetch('/api/positions/' + encodeURIComponent(id), { method: 'DELETE' });
                    if (!resp.ok && resp.status !== 404) throw new Error('HTTP ' + resp.status);
                    fetchAndRender(false);
                } catch (err) {
                    delBtn.disabled = false;
                    alert('No se pudo eliminar: ' + err.message);
                }
            }
        });
    }

    function initPositionModal() {
        const modalEl = document.getElementById('positionModal');
        const form = document.getElementById('positionForm');
        if (!modalEl || !form) return;
        const symInput = document.getElementById('posSymbol');
        const entryInput = document.getElementById('posEntry');
        const qtyInput = document.getElementById('posQty');
        const dateInput = document.getElementById('posDate');
        const errBox = document.getElementById('positionError');
        const dlist = document.getElementById('trackedSymbolsList');

        function showPosError(msg) {
            if (errBox) { errBox.textContent = msg; errBox.style.display = ''; } else { alert(msg); }
        }

        modalEl.addEventListener('shown.bs.modal', async function () {
            if (errBox) { errBox.style.display = 'none'; errBox.textContent = ''; }
            if (dateInput && !dateInput.value) {
                const now = new Date();
                now.setMinutes(now.getMinutes() - now.getTimezoneOffset());
                dateInput.value = now.toISOString().slice(0, 16);
            }
            // Prefill con el símbolo de la última ventana abierta (antes: el del selector global).
            if (symInput && !symInput.value && priceWindows.length) {
                symInput.value = priceWindows[priceWindows.length - 1].symbol;
            }
            if (dlist) {
                try {
                    const resp = await fetch('/api/instruments/tracked');
                    if (resp.ok) {
                        const items = await resp.json();
                        dlist.innerHTML = items.map(function (s) {
                            return '<option value="' + escapeHtml(s.symbol) + '">' + escapeHtml(s.name || '') + '</option>';
                        }).join('');
                    }
                } catch (e) { }
            }
            if (symInput) symInput.focus();
        });

        form.addEventListener('submit', async function (e) {
            e.preventDefault();
            if (errBox) { errBox.style.display = 'none'; errBox.textContent = ''; }
            const symbol = (symInput.value || '').trim();
            const entryPrice = parseFloat(String(entryInput.value).replace(',', '.'));
            const quantity = parseFloat(String(qtyInput.value).replace(',', '.'));
            if (!symbol) { showPosError('Indica un símbolo.'); return; }
            if (!(entryPrice > 0)) { showPosError('El precio de entrada debe ser > 0.'); return; }
            if (!(quantity > 0)) { showPosError('La cantidad debe ser > 0.'); return; }
            const openedAt = (dateInput && dateInput.value) ? new Date(dateInput.value).toISOString() : null;

            const submitBtn = document.getElementById('positionSubmit');
            if (submitBtn) submitBtn.disabled = true;
            try {
                const resp = await fetch('/api/positions', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ symbol: symbol, entryPrice: entryPrice, quantity: quantity, openedAt: openedAt })
                });
                if (!resp.ok) throw new Error((await resp.text()) || ('HTTP ' + resp.status));
                window.bootstrap.Modal.getOrCreateInstance(modalEl).hide();
                form.reset();
                refreshTrackedCurrencies();
                createPriceWindow(symbol.toUpperCase());   // abre su ventana si no la tenía (HV-054)
                fetchAndRender(true);
            } catch (err) {
                showPosError('No se pudo crear la posición: ' + err.message);
            } finally {
                if (submitBtn) submitBtn.disabled = false;
            }
        });
    }

    // ── Importar histórico de compras (HV-018): modal CSV ──────────────────────
    function initImportModal() {
        const modalEl = document.getElementById('importModal');
        if (!modalEl) return;
        const fileInput = document.getElementById('importFile');
        const textInput = document.getElementById('importText');
        const resultBox = document.getElementById('importResult');
        const submitBtn = document.getElementById('importSubmit');

        function showResult(html, cls) {
            if (!resultBox) return;
            resultBox.style.display = '';
            resultBox.className = 'small ' + (cls || '');
            resultBox.innerHTML = html;
        }

        if (fileInput) {
            fileInput.addEventListener('change', function () {
                const f = fileInput.files && fileInput.files[0];
                if (!f) return;
                const reader = new FileReader();
                reader.onload = function () { if (textInput) textInput.value = String(reader.result || ''); };
                reader.readAsText(f);
            });
        }

        if (submitBtn) {
            submitBtn.addEventListener('click', async function () {
                const csv = (textInput && textInput.value || '').trim();
                if (!csv) { showResult('Pega un CSV o selecciona un fichero.', 'text-danger'); return; }
                submitBtn.disabled = true;
                showResult('Importando…', 'text-muted');
                try {
                    const resp = await fetch('/api/positions/import', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ csv: csv })
                    });
                    if (!resp.ok) throw new Error((await resp.text()) || ('HTTP ' + resp.status));
                    const r = await resp.json();
                    let html = '<div class="text-success">✓ Importadas: <strong>' + r.imported + '</strong></div>';
                    if (r.failed > 0) {
                        html += '<div class="text-danger mt-1">Fallidas: <strong>' + r.failed + '</strong></div>';
                        html += '<ul class="mb-0 mt-1">' + (r.errors || []).map(function (e) {
                            return '<li>Línea ' + e.line + ': ' + escapeHtml(e.message) + '</li>';
                        }).join('') + '</ul>';
                    }
                    showResult(html, '');
                    fetchAndRender(true);
                    fetchAccountHistory();
                } catch (e) {
                    showResult('No se pudo importar: ' + escapeHtml(e.message), 'text-danger');
                } finally {
                    submitBtn.disabled = false;
                }
            });
        }

        modalEl.addEventListener('hidden.bs.modal', function () {
            if (fileInput) fileInput.value = '';
            if (textInput) textInput.value = '';
            if (resultBox) { resultBox.style.display = 'none'; resultBox.innerHTML = ''; }
        });
    }

    // ── Caja / efectivo (HV-014): modal con movimientos + alta/baja ────────────
    function initCashModal() {
        const modalEl = document.getElementById('cashModal');
        if (!modalEl) return;
        const amountInput = document.getElementById('cashAmount');
        const currencyInput = document.getElementById('cashCurrency');
        const noteInput = document.getElementById('cashNote');
        const errBox = document.getElementById('cashError');
        const body = document.getElementById('cashMovementsBody');
        const netEl = document.getElementById('cashNetDeposits');

        function showErr(msg) { if (errBox) { errBox.textContent = msg; errBox.style.display = ''; } }
        function clearErr() { if (errBox) { errBox.style.display = 'none'; errBox.textContent = ''; } }

        async function loadMovements() {
            if (!body) return;
            try {
                const resp = await fetch('/api/cash/movements');
                if (!resp.ok) throw new Error('HTTP ' + resp.status);
                const items = await resp.json();
                // Aportado neto agrupado por divisa (no se mezclan monedas; el total en base está en el dashboard).
                if (netEl) {
                    const byCcy = {};
                    items.forEach(function (m) {
                        const c = (m.currency || 'EUR').toUpperCase();
                        byCcy[c] = (byCcy[c] || 0) + (Number(m.amount) || 0);
                    });
                    const parts = Object.keys(byCcy).sort().map(function (c) { return money(byCcy[c], c); });
                    netEl.textContent = parts.length ? parts.join(' · ') : money(0, 'EUR');
                }
                if (!items.length) {
                    body.innerHTML = '<tr><td colspan="4" class="text-center text-muted">Sin movimientos</td></tr>';
                    return;
                }
                body.innerHTML = items.map(function (m) {
                    return '<tr>'
                        + '<td><small>' + new Date(m.createdAt).toLocaleString('es-ES') + '</small></td>'
                        + '<td>' + escapeHtml(m.note || (m.amount >= 0 ? 'Ingreso' : 'Retirada')) + '</td>'
                        + '<td class="text-end ' + signClass(m.amount) + '">' + money(m.amount, m.currency) + '</td>'
                        + '<td class="text-end"><button type="button" class="btn btn-outline-danger btn-sm py-0" '
                        + 'data-cash-del="' + m.id + '" title="Eliminar movimiento">✕</button></td>'
                        + '</tr>';
                }).join('');
            } catch (e) {
                body.innerHTML = '<tr><td colspan="4" class="text-center text-danger">Error al cargar: ' + escapeHtml(e.message) + '</td></tr>';
            }
        }

        async function addMovement(sign) {
            clearErr();
            const amt = parseFloat(String(amountInput.value).replace(',', '.'));
            if (!(amt > 0)) { showErr('Indica un importe > 0.'); return; }
            const currency = ((currencyInput && currencyInput.value) || 'EUR').trim().toUpperCase() || 'EUR';
            try {
                const resp = await fetch('/api/cash', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ amount: sign * amt, note: noteInput.value || '', currency: currency })
                });
                if (!resp.ok) throw new Error((await resp.text()) || ('HTTP ' + resp.status));
                amountInput.value = '';
                noteInput.value = '';
                await loadMovements();
                fetchAndRender(false);
            } catch (e) {
                showErr('No se pudo registrar: ' + e.message);
            }
        }

        modalEl.addEventListener('shown.bs.modal', function () { clearErr(); loadMovements(); if (amountInput) amountInput.focus(); });
        document.getElementById('cashDepositBtn').addEventListener('click', function () { addMovement(1); });
        document.getElementById('cashWithdrawBtn').addEventListener('click', function () { addMovement(-1); });
        if (body) {
            body.addEventListener('click', async function (e) {
                const del = e.target.closest('[data-cash-del]');
                if (!del) return;
                if (!confirm('¿Eliminar este movimiento de caja?')) return;
                del.disabled = true;
                try {
                    const resp = await fetch('/api/cash/' + encodeURIComponent(del.getAttribute('data-cash-del')), { method: 'DELETE' });
                    if (!resp.ok && resp.status !== 404) throw new Error('HTTP ' + resp.status);
                    await loadMovements();
                    fetchAndRender(false);
                } catch (e2) {
                    del.disabled = false;
                    alert('No se pudo eliminar: ' + e2.message);
                }
            });
        }
    }

    // ── Selector de proveedor de datos (HV-033): cambio en caliente ────────────
    function initProviderControl() {
        const sel = document.getElementById('providerSelect');
        const hint = document.getElementById('providerHint');
        if (!sel) return;

        function refreshInfo() {
            fetch('/api/provider').then(function (r) { return r.ok ? r.json() : null; }).then(function (info) {
                if (!info) return;
                if (info.current) sel.value = info.current;
                const noKey = (info.current === 'TwelveData' && !info.twelveDataKeyConfigured)
                    || (info.current === 'AlphaVantage' && !info.alphaVantageKeyConfigured);
                if (hint) hint.textContent = noKey ? '⚠ sin API key' : '';
            }).catch(function () { });
        }
        refreshInfo();

        sel.addEventListener('change', function () {
            sel.disabled = true;
            fetch('/api/provider', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ provider: sel.value })
            }).then(function (r) {
                if (!r.ok) throw new Error('HTTP ' + r.status);
                return r.json();
            }).then(function () {
                refreshInfo();
                fetchAndRender(true);      // refresca estado/datos
                reloadAllPriceWindows();   // recarga el histórico de cada ventana con el nuevo proveedor
            }).catch(function () {
                if (hint) hint.textContent = 'Error al cambiar';
            }).finally(function () { sel.disabled = false; });
        });
    }

    // Divisas de los símbolos seguidos (HV-037): para mostrar la variación del rango
    // en la moneda del instrumento aunque no tenga una posición abierta.
    async function refreshTrackedCurrencies() {
        try {
            const resp = await fetch('/api/instruments/tracked');
            if (!resp.ok) return;
            const items = await resp.json();
            (items || []).forEach(function (s) { if (s.currency) symbolCurrency[s.symbol] = s.currency; });
        } catch (e) { }
    }

    // ── Dividendos (HV-050): registro que suma al PnL/efectivo ───────────────────
    function initDividendModal() {
        const modalEl = document.getElementById('dividendModal');
        if (!modalEl) return;
        const symInput = document.getElementById('divSymbol');
        const amtInput = document.getElementById('divAmount');
        const curInput = document.getElementById('divCurrency');
        const dateInput = document.getElementById('divDate');
        const errBox = document.getElementById('dividendError');
        const body = document.getElementById('dividendBody');
        const totalEl = document.getElementById('dividendTotal');
        const form = document.getElementById('dividendForm');

        function showErr(m) { if (errBox) { errBox.textContent = m; errBox.style.display = ''; } }
        function clearErr() { if (errBox) { errBox.style.display = 'none'; errBox.textContent = ''; } }

        async function load() {
            if (!body) return;
            try {
                const resp = await fetch('/api/dividends');
                if (!resp.ok) throw new Error('HTTP ' + resp.status);
                const items = await resp.json();
                if (totalEl) {
                    const byCcy = {};
                    items.forEach(function (d) { const c = (d.currency || 'EUR').toUpperCase(); byCcy[c] = (byCcy[c] || 0) + (Number(d.amount) || 0); });
                    const parts = Object.keys(byCcy).sort().map(function (c) { return money(byCcy[c], c); });
                    totalEl.textContent = parts.length ? parts.join(' · ') : money(0, 'EUR');
                }
                if (!items.length) { body.innerHTML = '<tr><td colspan="4" class="text-center text-muted">Sin dividendos</td></tr>'; return; }
                body.innerHTML = items.map(function (d) {
                    return '<tr>'
                        + '<td><small>' + new Date(d.receivedAt).toLocaleString('es-ES') + '</small></td>'
                        + '<td><strong>' + escapeHtml(d.symbol) + '</strong></td>'
                        + '<td class="text-end text-success">' + money(d.amount, d.currency) + '</td>'
                        + '<td class="text-end"><button type="button" class="btn btn-outline-danger btn-sm py-0" data-div-del="' + d.id + '" title="Eliminar">✕</button></td>'
                        + '</tr>';
                }).join('');
            } catch (e) {
                body.innerHTML = '<tr><td colspan="4" class="text-center text-danger">Error: ' + escapeHtml(e.message) + '</td></tr>';
            }
        }

        modalEl.addEventListener('shown.bs.modal', function () {
            clearErr();
            if (dateInput && !dateInput.value) {
                const now = new Date();
                now.setMinutes(now.getMinutes() - now.getTimezoneOffset());
                dateInput.value = now.toISOString().slice(0, 16);
            }
            if (symInput && !symInput.value && priceWindows.length) symInput.value = priceWindows[priceWindows.length - 1].symbol;
            load();
            if (symInput) symInput.focus();
        });

        form.addEventListener('submit', async function (e) {
            e.preventDefault();
            clearErr();
            const symbol = (symInput.value || '').trim();
            const amount = parseFloat(String(amtInput.value).replace(',', '.'));
            const currency = ((curInput && curInput.value) || 'EUR').trim().toUpperCase() || 'EUR';
            if (!symbol) { showErr('Indica un símbolo.'); return; }
            if (!(amount > 0)) { showErr('El importe debe ser > 0.'); return; }
            const receivedAt = (dateInput && dateInput.value) ? new Date(dateInput.value).toISOString() : null;
            try {
                const resp = await fetch('/api/dividends', {
                    method: 'POST', headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ symbol: symbol, amount: amount, currency: currency, receivedAt: receivedAt })
                });
                if (!resp.ok) throw new Error((await resp.text()) || ('HTTP ' + resp.status));
                amtInput.value = '';
                await load();
                fetchAndRender(false);   // el PnL/efectivo reflejan el dividendo
                fetchBreakdown();        // aparece en el desglose por periodo
            } catch (err) {
                showErr('No se pudo registrar: ' + err.message);
            }
        });

        if (body) {
            body.addEventListener('click', async function (e) {
                const del = e.target.closest('[data-div-del]');
                if (!del) return;
                if (!confirm('¿Eliminar este dividendo?')) return;
                del.disabled = true;
                try {
                    const resp = await fetch('/api/dividends/' + encodeURIComponent(del.getAttribute('data-div-del')), { method: 'DELETE' });
                    if (!resp.ok && resp.status !== 404) throw new Error('HTTP ' + resp.status);
                    await load();
                    fetchAndRender(false);
                    fetchBreakdown();
                } catch (e2) { del.disabled = false; alert('No se pudo eliminar: ' + e2.message); }
            });
        }
    }

    // ── Modo visor puro (HV-046): solo lectura, oculta los controles de edición ──
    function initViewerMode() {
        try { if (localStorage.getItem('viewerMode') === '1') viewerMode = true; } catch (e) { }
        const container = document.querySelector('.dashboard');
        const btn = document.getElementById('viewerToggle');
        function apply() {
            if (container) container.classList.toggle('viewer-mode', viewerMode);
            if (btn) { btn.classList.toggle('active', viewerMode); btn.setAttribute('aria-pressed', viewerMode ? 'true' : 'false'); }
        }
        apply();
        if (btn) {
            btn.addEventListener('click', function () {
                viewerMode = !viewerMode;
                try { localStorage.setItem('viewerMode', viewerMode ? '1' : '0'); } catch (e) { }
                apply();
                fetchAndRender(false);   // re-render tablas (aparece/desaparece la columna de acciones)
            });
        }
    }

    // ── Modo privacidad (HV-051): desenfoca los importes sensibles (curiosos detrás) ──
    // Solo CSS (toggle de clase); no necesita re-render porque los datos ya están en el DOM.
    function initPrivacyMode() {
        try { if (localStorage.getItem('privacyMode') === '1') privacyMode = true; } catch (e) { }
        const container = document.querySelector('.dashboard');
        const btn = document.getElementById('privacyToggle');
        function apply() {
            if (container) container.classList.toggle('privacy-mode', privacyMode);
            if (btn) { btn.classList.toggle('active', privacyMode); btn.setAttribute('aria-pressed', privacyMode ? 'true' : 'false'); }
        }
        apply();
        if (btn) {
            btn.addEventListener('click', function () {
                privacyMode = !privacyMode;
                try { localStorage.setItem('privacyMode', privacyMode ? '1' : '0'); } catch (e) { }
                apply();
            });
        }
    }

    initViewerMode();
    initPrivacyMode();
    initProviderControl();
    initChartRefreshControl();
    initHistoryControl();
    initInstrumentSearch();
    initPositionsActions();
    initPositionModal();
    initCashModal();
    initDividendModal();
    initImportModal();
    initAccountHistoryControl();
    const savedWindows = initPriceWindows();
    // Primer pintado completo. Cargamos las divisas primero para que la variación del rango
    // salga en su moneda desde el primer render; después restauramos las ventanas de precios
    // de la sesión anterior (o abrimos una con el primer símbolo seguido si no había ninguna).
    refreshTrackedCurrencies().finally(function () {
        fetchAndRender(true).then(function () {
            if (savedWindows.length) {
                savedWindows.forEach(function (w) {
                    createPriceWindow(w.symbol, Object.assign({}, w, { deferLoad: true }));
                });
                reloadAllPriceWindows();   // carga en serie, no N peticiones a la vez
            } else if (lastSeries.length) {
                // Arranque limpio: una ventana de cortesía con el primer símbolo seguido,
                // heredando los ajustes del gráfico único anterior a HV-054.
                createPriceWindow(lastSeries[0].symbol, legacyChartPrefs());
            }
        });
    });
    fetchAccountHistory(); // histórico del valor de cuenta (refresco propio, los snapshots son cada pocos min)
    fetchMetrics();        // métricas avanzadas (HV-048)
    fetchBreakdown();      // resultados por periodo (HV-049)
    metricsTimer = setInterval(function () { fetchAndRender(false); }, METRICS_MS);
    setInterval(fetchAccountHistory, 60000);
    setInterval(fetchMetrics, 60000);
    setInterval(fetchBreakdown, 60000);
    setInterval(refreshTrackedCurrencies, 60000);
})();
