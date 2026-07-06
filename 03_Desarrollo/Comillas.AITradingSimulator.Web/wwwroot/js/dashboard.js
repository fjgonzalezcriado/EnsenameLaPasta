(function () {
    'use strict';

    const REFRESH_MS = 3000;
    const COLORS = ['#0d6efd', '#fd7e14', '#198754', '#dc3545', '#6f42c1', '#20c997'];
    // En modo "En vivo" los ticks se capturan cada ~30 s, pero entre sesiones (app apagada)
    // hay huecos de horas/días. Si dos ticks consecutivos distan más de esto, cortamos la
    // línea (punto nulo) para no dibujar una diagonal recta engañosa sobre el hueco.
    const LIVE_GAP_MS = 5 * 60 * 1000;
    let priceChart = null;
    let selectedSymbol = null;   // símbolo activo en el gráfico, o 'ALL'
    let lastSeries = [];         // última priceSeries recibida (para re-render al cambiar de símbolo)
    let historyPoints = 50;      // nº de puntos de histórico a pedir por símbolo
    const HISTORY_ALLOWED = [50, 250, 1000, 5000];
    let chartMode = 'LIVE';      // 'LIVE' (ticks simulados) | rango Yahoo ('1D','1M',…)
    let baseCurrency = 'EUR';    // divisa base de los totales (HV-020); para el PnL convertido por fila (HV-022)

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
        const badge = document.getElementById('providerBadge');
        if (badge) {
            const yahoo = d.providerType === 'YahooFinance';
            badge.textContent = yahoo ? 'Yahoo Finance (real)' : 'RandomWalk (simulado)';
            badge.className = 'badge ' + (yahoo ? 'text-bg-success' : 'text-bg-secondary');
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

    function renderOpenTrades(trades) {
        const tbody = document.getElementById('openTradesBody');
        if (!trades || trades.length === 0) {
            tbody.innerHTML = '<tr><td colspan="9" class="text-center text-muted">Sin posiciones abiertas</td></tr>';
            return;
        }
        tbody.innerHTML = trades.map(function (t) {
            return '<tr>'
                + '<td><strong>' + t.symbol + '</strong></td>'
                + '<td><span class="badge text-bg-secondary">' + (t.currency || '—') + '</span></td>'
                + '<td>' + num(t.entryPrice) + '</td>'
                + '<td>' + num(t.currentPrice) + '</td>'
                + '<td>' + num(t.quantity) + '</td>'
                + '<td class="' + signClass(t.unrealizedPnL) + '">' + pnlCell(t.unrealizedPnL, t.currency, t.unrealizedPnLBase) + '</td>'
                + '<td class="' + signClass(t.returnPct) + '">' + pctSigned(t.returnPct) + '</td>'
                + '<td><small>' + new Date(t.createdAt).toLocaleString('es-ES') + '</small></td>'
                + '<td class="text-end text-nowrap">'
                + '<button type="button" class="btn btn-outline-primary btn-sm py-0 me-1" '
                + 'data-close="' + t.id + '" data-symbol="' + t.symbol + '" data-price="' + t.currentPrice + '">Cerrar</button>'
                + '<button type="button" class="btn btn-outline-danger btn-sm py-0" '
                + 'data-del="' + t.id + '" data-symbol="' + t.symbol + '" title="Eliminar del seguimiento">✕</button>'
                + '</td>'
                + '</tr>';
        }).join('');
    }

    function renderClosedTrades(trades) {
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
                const meta = chart.getDatasetMeta(i);
                if (meta.hidden || !meta.data || meta.data.length === 0) return;
                const last = meta.data[meta.data.length - 1];
                const raw = ds.data[ds.data.length - 1];
                if (!last || !raw || !Number.isFinite(last.y)) return;

                const text = priceFmt(raw.y);
                const y = Math.max(area.top + h / 2, Math.min(area.bottom - h / 2, last.y));
                const w = Math.max(axisRight - axisLeft - 1, ctx.measureText(text).width + 8);

                ctx.save();
                ctx.font = '600 11px sans-serif';
                ctx.fillStyle = ds.borderColor;

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
    function ensureSymbolOptions(series) {
        const sel = document.getElementById('symbolSelect');
        if (!sel) return;
        const symbols = (series || []).map(function (s) { return s.symbol; });
        const want = ['ALL'].concat(symbols);
        const have = Array.from(sel.options).map(function (o) { return o.value; });
        if (have.join('|') !== want.join('|')) {
            sel.innerHTML = '<option value="ALL">Todos</option>'
                + symbols.map(function (s) { return '<option value="' + s + '">' + s + '</option>'; }).join('');
        }
        // Si la selección actual ya no es válida, usar el primer símbolo (vista individual limpia).
        if (want.indexOf(selectedSymbol) === -1) {
            selectedSymbol = symbols.length ? symbols[0] : 'ALL';
        }
        if (sel.value !== selectedSymbol) sel.value = selectedSymbol;
    }

    // Inserta un punto nulo entre ticks separados por más de LIVE_GAP_MS para que
    // Chart.js corte la línea sobre los huecos entre sesiones (evita la diagonal falsa).
    function insertLiveGaps(points) {
        if (points.length < 2) return points;
        const out = [];
        for (let i = 0; i < points.length; i++) {
            if (i > 0 && (points[i].x - points[i - 1].x) > LIVE_GAP_MS) {
                out.push({ x: points[i - 1].x + Math.floor((points[i].x - points[i - 1].x) / 2), y: null });
            }
            out.push(points[i]);
        }
        return out;
    }

    function renderChart(series) {
        if (typeof Chart === 'undefined') {
            console.error('[dashboard] Chart.js no está cargado (¿CDN bloqueado o sin conexión?).');
            return;
        }

        const datasets = (series || []).map(function (s, idx) {
            const data = (s.points || [])
                .map(function (p) { return { x: new Date(p.timestamp).getTime(), y: Number(p.price) }; })
                .filter(function (pt) { return Number.isFinite(pt.x) && Number.isFinite(pt.y); });
            return {
                label: s.symbol,
                data: chartMode === 'LIVE' ? insertLiveGaps(data) : data,
                spanGaps: false,   // no unir a través de puntos nulos (huecos entre sesiones)
                borderColor: COLORS[idx % COLORS.length],
                backgroundColor: 'transparent',
                tension: 0.1,
                pointRadius: 0,
                borderWidth: 2
            };
        }).filter(function (ds) {
            // Filtra por símbolo seleccionado (o todos), descartando series vacías.
            return ds.data.length > 0 && (selectedSymbol === 'ALL' || ds.label === selectedSymbol);
        });

        if (datasets.length === 0) {
            console.warn('[dashboard] Sin puntos de precio para graficar todavía.');
            return;
        }

        const xb = computeXBounds(datasets);
        const unit = timeUnitFor(xb);

        if (priceChart === null) {
            const ctx = document.getElementById('chartPrices').getContext('2d');
            priceChart = new Chart(ctx, {
                type: 'line',
                data: { datasets: datasets },
                plugins: [currentValuePlugin],
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    animation: false,
                    interaction: { mode: 'nearest', intersect: false },
                    scales: {
                        x: {
                            type: 'time',
                            time: { unit: unit, displayFormats: DISPLAY_FORMATS },
                            grid: { display: true, color: function () { return gridColor(); } },
                            ticks: { maxRotation: 0, autoSkip: true, color: function () { return axisTextColor(); } },
                            min: xb ? xb.min : undefined,
                            max: xb ? xb.max : undefined
                        },
                        y: {
                            type: 'linear',
                            position: 'right',
                            grid: { display: true, color: function () { return gridColor(); } },
                            ticks: { callback: function (v) { return priceFmt(v); }, color: function () { return axisTextColor(); } }
                        }
                    },
                    plugins: {
                        legend: { position: 'top' },
                        tooltip: {
                            callbacks: {
                                label: function (ctx) { return ctx.dataset.label + ': ' + priceFmt(ctx.parsed.y); }
                            }
                        }
                    }
                }
            });
        } else {
            priceChart.data.datasets = datasets;
            priceChart.options.scales.x.time.unit = unit;
            if (xb) {
                priceChart.options.scales.x.min = xb.min;
                priceChart.options.scales.x.max = xb.max;
            }
            priceChart.update();
        }

        positionCountdown();
    }

    // Coloca la cuenta atrás justo debajo de la etiqueta de valor de la serie activa.
    function positionCountdown() {
        const el = document.getElementById('tickCountdown');
        if (!el || !priceChart) return;
        if (chartMode !== 'LIVE') { el.style.display = 'none'; return; }
        const datasets = priceChart.data.datasets || [];
        if (datasets.length === 0) { el.style.display = 'none'; return; }
        let idx = 0;
        if (selectedSymbol && selectedSymbol !== 'ALL') {
            const i = datasets.findIndex(function (d) { return d.label === selectedSymbol; });
            if (i >= 0) idx = i;
        }
        const data = datasets[idx].data || [];
        const yScale = priceChart.scales.y;
        if (data.length === 0 || !yScale) { el.style.display = 'none'; return; }
        const px = yScale.getPixelForValue(data[data.length - 1].y);
        if (!Number.isFinite(px)) { el.style.display = 'none'; return; }
        el.style.top = (px + 11) + 'px'; // bajo la etiqueta de valor
        el.style.display = 'block';
    }

    // ── Evolución del valor de cuenta (HV-017) ─────────────────────────────
    let accountChart = null;
    let accountHistoryPoints = 200;
    const ACCOUNT_HISTORY_ALLOWED = [50, 200, 1000, 5000];

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
            hint.textContent = data.length + ' snapshot(s) · último valor ' + eur(last.accountValue) + ' (' + pctSigned(last.returnPct) + ')';
        }

        const accountData = data.map(function (p) { return { x: p.x, y: p.av }; });
        const depositsData = data.map(function (p) { return { x: p.x, y: p.nd }; });
        const datasets = [
            {
                label: 'Valor de cuenta', data: accountData,
                borderColor: '#198754', backgroundColor: 'rgba(25,135,84,0.10)',
                fill: true, tension: 0.15, pointRadius: 0, borderWidth: 2
            },
            {
                label: 'Aportado neto', data: depositsData,
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

    async function fetchAccountHistory() {
        try {
            const resp = await fetch('/api/account/history?points=' + accountHistoryPoints);
            if (!resp.ok) { console.warn('account history fetch failed:', resp.status); return; }
            renderAccountChart(await resp.json());
        } catch (err) {
            console.error('account history fetch error', err);
        }
    }

    function initAccountHistoryControl() {
        try {
            const saved = parseInt(localStorage.getItem('accountHistoryPoints'), 10);
            if (ACCOUNT_HISTORY_ALLOWED.indexOf(saved) !== -1) accountHistoryPoints = saved;
        } catch (e) { }
        const sel = document.getElementById('accountHistorySelect');
        if (sel) {
            sel.value = String(accountHistoryPoints);
            sel.addEventListener('change', function () {
                const next = parseInt(sel.value, 10);
                accountHistoryPoints = ACCOUNT_HISTORY_ALLOWED.indexOf(next) !== -1 ? next : 200;
                try { localStorage.setItem('accountHistoryPoints', String(accountHistoryPoints)); } catch (e) { }
                fetchAccountHistory();
            });
        }
    }

    // Métricas/estado/tablas siempre en vivo; el gráfico se redibuja según su
    // propio intervalo (compuerta temporal). Una sola petición por ciclo.
    const METRICS_MS = 3000;
    const ALLOWED_MS = [3000, 30000, 60000, 120000, 300000];
    let chartIntervalMs = REFRESH_MS;
    let lastChartAt = 0;
    let metricsTimer = null;

    async function fetchAndRender(forceChart) {
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
            // Gráfico: solo en modo LIVE y según el intervalo elegido (en histórico no se toca).
            if (chartMode === 'LIVE') {
                const now = Date.now();
                if (forceChart || (now - lastChartAt) >= chartIntervalMs) {
                    renderChart(lastSeries);
                    lastChartAt = now;
                }
            }
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

    // ── Selector de símbolo del gráfico ─────────────────────────────────────
    function initSymbolControl() {
        try {
            const saved = localStorage.getItem('chartSymbol');
            if (saved) selectedSymbol = saved;
        } catch (e) { }

        const sel = document.getElementById('symbolSelect');
        if (sel) {
            sel.addEventListener('change', function () {
                selectedSymbol = sel.value;
                try { localStorage.setItem('chartSymbol', selectedSymbol); } catch (e) { }
                if (chartMode === 'LIVE') renderChart(lastSeries); // rescala al nuevo símbolo
                else loadHistory(chartMode);                        // recarga histórico del símbolo
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

    // ── Barra de rango temporal: histórico real de Yahoo (/api/history) ─────────
    async function loadHistory(range) {
        const hint = document.getElementById('rangeHint');
        // Símbolos a pedir: el seleccionado, o todos los del feed en vivo si "Todos".
        const symbols = (selectedSymbol && selectedSymbol !== 'ALL')
            ? [selectedSymbol]
            : (lastSeries || []).map(function (s) { return s.symbol; });
        if (symbols.length === 0) { if (hint) hint.textContent = 'Sin símbolos disponibles.'; return; }

        if (hint) hint.textContent = 'Cargando histórico ' + range + '…';
        try {
            const results = await Promise.all(symbols.map(function (sym) {
                return fetch('/api/history?symbol=' + encodeURIComponent(sym) + '&range=' + encodeURIComponent(range))
                    .then(function (r) { return r.ok ? r.json() : null; })
                    .catch(function () { return null; });
            }));
            const series = results.filter(function (s) { return s && s.points && s.points.length; });
            if (series.length === 0) {
                if (hint) hint.textContent = 'Sin datos de histórico para ' + range + '.';
                return;
            }
            renderChart(series);
            if (hint) hint.textContent = 'Histórico Yahoo · ' + range;
        } catch (e) {
            console.error('Error cargando histórico', e);
            if (hint) hint.textContent = 'Error cargando histórico.';
        }
    }

    function initRangeBar() {
        const bar = document.getElementById('rangeBar');
        if (!bar) return;
        bar.addEventListener('click', function (e) {
            const btn = e.target.closest('button[data-range]');
            if (!btn) return;
            Array.prototype.forEach.call(bar.querySelectorAll('button'), function (b) { b.classList.remove('active'); });
            btn.classList.add('active');
            const range = btn.getAttribute('data-range');
            const hint = document.getElementById('rangeHint');
            if (range === 'LIVE') {
                chartMode = 'LIVE';
                if (hint) hint.textContent = '';
                renderChart(lastSeries);    // vuelve a la vista simulada al instante
                lastChartAt = Date.now();   // reinicia la cuenta atrás del gráfico
            } else {
                chartMode = range;
                loadHistory(range);
            }
        });
    }

    // Cuenta atrás al próximo refresco del gráfico (relativa al selector "Gráfico cada").
    function tickCountdownLoop() {
        const el = document.getElementById('tickCountdown');
        if (!el || el.style.display === 'none' || chartMode !== 'LIVE' || chartIntervalMs <= 0) return;
        const rem = Math.max(0, lastChartAt + chartIntervalMs - Date.now());
        el.textContent = '⏱ ' + (rem / 1000).toFixed(1) + 's';
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
            selectedSymbol = symbol;
            try { localStorage.setItem('chartSymbol', selectedSymbol); } catch (e) { }
            // Cierra la modal del buscador tras añadir.
            const modalEl = document.getElementById('searchModal');
            if (modalEl && window.bootstrap) {
                window.bootstrap.Modal.getOrCreateInstance(modalEl).hide();
            }
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

    function initRemoveSymbol() {
        const btn = document.getElementById('removeSymbolBtn');
        if (!btn) return;
        btn.addEventListener('click', async function () {
            if (!selectedSymbol || selectedSymbol === 'ALL') {
                alert('Selecciona un símbolo concreto para dejar de seguirlo.');
                return;
            }
            const sym = selectedSymbol;
            if (!confirm('¿Dejar de seguir ' + sym + '? Se borrará su histórico de ticks.')) return;
            btn.disabled = true;
            try {
                const resp = await fetch('/api/instruments/track?symbol=' + encodeURIComponent(sym), { method: 'DELETE' });
                if (!resp.ok && resp.status !== 404) throw new Error('HTTP ' + resp.status);
                selectedSymbol = 'ALL';
                try { localStorage.setItem('chartSymbol', selectedSymbol); } catch (e) { }
                chartMode = 'LIVE';
                fetchAndRender(true);
            } catch (e) {
                alert('No se pudo quitar el símbolo: ' + e.message);
            } finally {
                btn.disabled = false;
            }
        });
    }

    // ── Posiciones reales del usuario (HV-013): acciones por fila + modal de alta ──
    function initPositionsActions() {
        const tbody = document.getElementById('openTradesBody');
        if (!tbody) return;
        tbody.addEventListener('click', async function (e) {
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
            if (symInput && !symInput.value && selectedSymbol && selectedSymbol !== 'ALL') {
                symInput.value = selectedSymbol;
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
                selectedSymbol = symbol.toUpperCase();
                try { localStorage.setItem('chartSymbol', selectedSymbol); } catch (e) { }
                chartMode = 'LIVE';
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

    initChartRefreshControl();
    initSymbolControl();
    initHistoryControl();
    initRangeBar();
    initInstrumentSearch();
    initRemoveSymbol();
    initPositionsActions();
    initPositionModal();
    initCashModal();
    initImportModal();
    initAccountHistoryControl();
    fetchAndRender(true); // primer pintado completo (incluye gráfico)
    fetchAccountHistory(); // histórico del valor de cuenta (refresco propio, los snapshots son cada pocos min)
    metricsTimer = setInterval(function () { fetchAndRender(false); }, METRICS_MS);
    setInterval(fetchAccountHistory, 60000);
    setInterval(tickCountdownLoop, 150);
})();
