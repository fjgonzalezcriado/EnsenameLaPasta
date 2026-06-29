# Lecciones Aprendidas del Proyecto

> **INSTRUCCIONES PARA CLAUDE**: Este archivo documenta patrones, errores y particularidades
> descubiertos durante el desarrollo. Consulta este archivo al inicio de cada sesion para
> evitar repetir errores y aprovechar lo que ya funciona. Actualiza con `/sesion` al final
> de cada sesion si hay nuevas lecciones.

---

## Resumen Rapido (Top 5)

> Las 5 lecciones mas importantes del proyecto. Actualizar cuando cambien las prioridades.

| # | Leccion | Categoria |
|---|---------|-----------|
| 1 | Tras `dotnet run` (smoke), matar el proceso `*.Web` antes de recompilar o el build falla (DLL bloqueada, MSB3027) | Error |
| 2 | Editar valores con `>` en `ESTADO_PROYECTO.json` falla con Edit (PowerShell los escribe como `>`); usar PowerShell regex + validar con `ConvertFrom-Json` | Tecnica |
| 3 | Conversión FX a divisa base: convertir por posición y **no redondear** los agregados monetarios preserva el invariante `accountValue = netDeposits + totalPnL` | Patron |
| 4 | Al extender entidades/servicios, usar parámetros opcionales (default) y métodos de interfaz por defecto para no romper llamadas/tests existentes | Patron |
| 5 | SQLite: `DELETE` no encoge el fichero (usar `VACUUM`); medir tamaño con `PRAGMA page_count × page_size`, no con `FileInfo` (el WAL falsea) | Tecnica |

---

## 1. Patrones del Proyecto

> Patrones que funcionan bien en este proyecto y que Claude debe reutilizar.

### PAT-001: [Nombre del patron]

**Contexto**: [Donde y cuando aplicar este patron]
**Patron**: [Descripcion concisa del patron]
**Ejemplo**: [Referencia a archivo o codigo]
**Fecha**: [YYYY-MM-DD]

<!--
Ejemplos de patrones:
- "Para validaciones, siempre usamos FluentValidation con mensajes en castellano"
- "Los DTOs de respuesta siempre incluyen campo 'Id' y 'FechaModificacion'"
- "Los endpoints de listado siempre devuelven PaginatedResult<T>"
-->

---

## 2. Errores Corregidos

> Errores que ya ocurrieron y como se solucionaron. Claude debe evitar reintroducirlos.

### ERR-001: [Descripcion breve del error]

**Sintoma**: [Como se manifesto el error]
**Causa raiz**: [Que lo provoco]
**Solucion**: [Como se corrigio]
**Prevencion**: [Que hacer para que no vuelva a ocurrir]
**Fecha**: [YYYY-MM-DD]

<!--
Ejemplos de errores:
- "Connection string sin TrustServerCertificate=True falla en SQL Server 2022"
- "Usar .Result en metodo async provoca deadlock en controllers"
- "Faltaba AsNoTracking en queries de solo lectura, causando lentitud"
-->

---

## 3. Particularidades Tecnicas

> Caracteristicas especificas de este proyecto que no son evidentes del codigo.

### TEC-001: [Particularidad]

**Descripcion**: [Que hace especial o diferente a este proyecto]
**Impacto**: [Como afecta al desarrollo]
**Referencia**: [Archivo o documentacion relacionada]
**Fecha**: [YYYY-MM-DD]

<!--
Ejemplos de particularidades:
- "La BD usa intercalacion SQL_Latin1_General_CP1250_CI_AS (no la default)"
- "El servidor de produccion no tiene acceso a internet (sin NuGet restore)"
- "Hay un trigger en tabla Estudiante que actualiza FechaModificacion automaticamente"
- "La autenticacion Azure AD usa tenant multi-organizacion"
-->

---

## 4. Preferencias del Equipo

> Preferencias explicitas del equipo que Claude debe respetar.

### PREF-001: [Preferencia]

**Descripcion**: [Que prefiere el equipo]
**Motivo**: [Por que se prefiere asi]
**Fecha**: [YYYY-MM-DD]

<!--
Ejemplos de preferencias:
- "Preferimos mensajes de error en castellano para el usuario final"
- "Los logs deben ser en ingles para compatibilidad con herramientas"
- "No usar operador ternario en condiciones complejas (preferir if/else)"
- "Los commits siempre en castellano con formato Conventional Commits"
-->

---

## Relacion con Otros Archivos

| Archivo | Relacion con LECCIONES.md |
|---------|--------------------------|
| `DECISIONES.md` | Decisiones arquitectonicas formales (ADRs). LECCIONES es mas operativo |
| `DEUDA_TECNICA.md` | Issues conocidos pendientes. LECCIONES documenta lo ya resuelto |
| `DEPENDENCIAS.md` | Stack tecnico. LECCIONES documenta particularidades de uso |
| `SESION_ACTUAL.md` | Contexto de sesion. `/sesion` actualiza ambos archivos |
| `CLAUDE.md` | Reglas generales. LECCIONES son especificas de este proyecto |

---

## Como Actualizar Este Archivo

### Automaticamente
- Al ejecutar `/sesion`, Claude revisa si hay nuevas lecciones de la sesion actual

### Manualmente
- Cuando se descubre un patron util: anadir en seccion 1
- Cuando se corrige un bug no trivial: anadir en seccion 2
- Cuando se descubre una particularidad: anadir en seccion 3
- Cuando el equipo expresa una preferencia: anadir en seccion 4

### Mantenimiento
- Revisar periodicamente que las lecciones sigan siendo relevantes
- Mover lecciones obsoletas a un bloque de comentario HTML
- Mantener el Top 5 actualizado con las lecciones mas impactantes

---

## Lecciones registradas

### L-001 (Error/Frontend) — Chart.js `update('none')` + hover → crash en `PointElement.inRange`
- **Fecha**: 2026-06-15 (durante smoke test de HV-008)
- **Síntoma**: al pasar el ratón sobre el gráfico del dashboard, excepción JS con mensaje vacío y stack en `PointElement.inRange` (Chart.js 4.4.0).
- **Causa**: `dashboard.js` refrescaba cada 3s con `priceChart.update('none')`. El modo `'none'` no recalcula las `options` de los `PointElement` añadidos cuando la serie crece (warmup 1→50 puntos); al hacer hover (interaction `nearest`), `inRange` lee `this.options.hitRadius` sobre un punto con `options=undefined` → revienta. Intermitente (solo durante el crecimiento de la serie).
- **Fix**: usar `priceChart.update()` (modo por defecto, recalcula opciones) + `animation: false` en options (evita parpadeo del polling). Además, blindaje defensivo: `y: Number(p.price)` + `.filter(Number.isFinite)`.
- **Regla general**: en charts con polling, no usar `update('none')` si la longitud de los datasets cambia y hay interacción de hover. Preferir `update()` con `animation:false`.

### L-002 (Técnica/SQLite) — Retención por tamaño: `DELETE` no encoge el fichero; medir con PRAGMA, no por tamaño de fichero
- **Fecha**: 2026-06-15 (HV: retención de BD)
- **Contexto**: `DatabaseRetentionService` purga `MarketTick` antiguos al superar un límite de tamaño.
- **Aprendizajes**:
  1. En SQLite, `DELETE` libera páginas pero **NO reduce el fichero**; hay que ejecutar `VACUUM` para devolver el espacio al SO.
  2. **No medir el tamaño con `FileInfo` del `.db` + `-wal`**: el WAL transitorio se infla bajo escritura intensa y da lecturas falsas (provocaba disparos repetidos y log "compactada a 4 MB" cuando en realidad bajaba). Medir el tamaño lógico con `PRAGMA page_count × page_size` (refleja el VACUUM y excluye el WAL).
  3. Borrado masivo eficiente sin cargar entidades: calcular un `Timestamp` de corte (`OrderBy(Timestamp).Skip(n).Select(...).FirstOrDefault()`) y `Where(t => t.Timestamp <= cutoff).ExecuteDeleteAsync()`.
  4. `VACUUM` convive con el generador de ticks (SQLite serializa), no hubo "database is locked" en pruebas.

### L-003 (Error/Build) — Proceso `*.Web` del smoke bloquea las DLLs y rompe el siguiente build
- **Fecha**: 2026-06-29 (sesión HV-015..023)
- **Síntoma**: `dotnet test`/`build` falla con `MSB3026`/`MSB3027` "The process cannot access the file ...Infrastructure.dll because it is being used by another process: Comillas.AITradingSimulator.Web (PID)".
- **Causa**: tras un smoke con `dotnet run` en background, matar el wrapper (`kill <pid>` de bash) no siempre mata el proceso real `Comillas.AITradingSimulator.Web`, que queda vivo bloqueando `bin/Debug/net10.0`.
- **Fix/Prevención**: parar la app antes de recompilar — `Get-Process -Name "Comillas.AITradingSimulator.Web" | Stop-Process -Force` y confirmar 0 procesos. La app ignora `ASPNETCORE_URLS` y usa el puerto de `launchSettings.json` (5177); sacar el puerto real del log.

### L-004 (Técnica/DURAN) — `ESTADO_PROYECTO.json` escapa `>` como `>`; Edit literal falla
- **Fecha**: 2026-06-29
- **Síntoma**: editar con la herramienta Edit cualquier valor que contenga `>` (p.ej. `HY9H.F->EUR`, `{FROM}{TO}=X`) falla con "String to replace not found".
- **Causa**: el JSON lo serializa PowerShell (`ConvertTo-Json`), que escapa `>`, `<` y `'` como `>`, `<`, `'`. El texto en disco no coincide con lo que se ve/teclea.
- **Fix**: para esos valores, editar con PowerShell por regex sobre el texto crudo (`[regex]::Replace(...)`), reescribir con `Set-Content -Encoding UTF8 -NoNewline` y validar con `ConvertFrom-Json`. Para campos sin caracteres especiales, Edit funciona normal.

### L-005 (Patrón/FX) — Conversión a divisa base preservando el invariante de cuenta
- **Fecha**: 2026-06-29 (HV-020/021/022)
- **Contexto**: cartera multidivisa; los totales deben expresarse en una divisa base sin romper `accountValue = netDeposits + totalPnL`.
- **Patrón**:
  1. Convertir **cada posición/aportación** por el tipo de SU divisa (`Σ valor·rate(ccy→base)`), no el total por un único tipo.
  2. **No redondear** los agregados monetarios (solo se redondean ratios como `returnPct`/`winrate`); así el invariante se mantiene exacto en los tests.
  3. Tipos vía Yahoo `{FROM}{TO}=X`; caché por par con TTL + **refresco en background** (`FxRefreshService`) para sacar el HTTP del hot path; degradar a último valor/1 si falla (nunca lanzar en el dashboard).

### L-006 (Patrón/Compatibilidad) — Extender sin romper: parámetros opcionales y métodos de interfaz por defecto
- **Fecha**: 2026-06-29 (HV-019/021/023)
- **Patrón**: al añadir capacidades a entidades/servicios usados por muchos tests:
  - parámetros nuevos como **opcionales con default** (`Create(..., currency = "EUR")`, `AddAsync(..., currency = "EUR")`) → las llamadas/tests existentes compilan sin tocarse;
  - métodos nuevos de interfaz con **implementación por defecto** (`RefreshAsync => GetRateAsync(...)`) → los stubs de test no necesitan implementarlos;
  - campos nuevos en `record` posicionales: insertarlos sin reordenar los previos; ningún test construye los DTO directamente (van por el servicio), así que el cambio es seguro.
- **Migraciones EF de columnas nuevas**: poner `defaultValue` (p.ej. `"EUR"`) para que las filas existentes queden coherentes.

---

*Archivo de lecciones aprendidas - STIC.IA v3.7.0*
