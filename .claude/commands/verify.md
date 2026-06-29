Ejecutar pipeline de verificacion de 7 fases antes de commit o PR

# /verify - Pipeline de Verificacion Pre-Commit

## REGLAS CRITICAS

- Si la Fase 1 (Build) o Fase 4 (Tests) fallan: DETENER el pipeline inmediatamente (short-circuit)
- Si alguna fase tiene FAIL: el resultado global es FAIL
- Si solo hay WARN: el resultado global es WARN (se puede proceder con precaucion)
- Si todo es PASS: el resultado global es PASS (listo para commit/PR)
- Mostrar SIEMPRE la tabla resumen al final

---

## 1. Detectar solucion

Buscar archivos `.sln` o `.slnx` en el proyecto. Si hay varios, consultar `_duran/ESTADO_PROYECTO.json` campo `soluciones.solucionActiva`. Si no hay solucion activa, preguntar al usuario.

Guardar la ruta de la solucion en variable `$SLN` para los pasos siguientes.

---

## 2. Fase 1: Build

Ejecutar:
```bash
dotnet build "$SLN" --no-incremental 2>&1
```

- Si el exit code es distinto de 0: registrar **FAIL**, contar errores, y **DETENER** el pipeline.
- Si hay 0 errores: registrar **PASS**.
- Guardar el numero de warnings para la Fase 2.

---

## 3. Fase 2: Diagnosticos

Intentar primero usar MCP `get_diagnostics` si esta disponible. Si no:

```bash
dotnet build "$SLN" -warnaserror 2>&1
```

Contar el numero de warnings/diagnosticos.

- 0 warnings: **PASS**
- 1+ warnings: **WARN** con el conteo

---

## 4. Fase 3: Anti-patrones

Intentar primero usar MCP `detect_antipatterns` si esta disponible. Si no, buscar manualmente en archivos `.cs` (excluyendo `obj/`, `bin/`, `Migrations/`):

| Patron | Buscar | Severidad |
|--------|--------|-----------|
| DateTime.Now | `DateTime.Now` (usar `DateTime.UtcNow` o `TimeProvider`) | WARN |
| new HttpClient | `new HttpClient(` (usar IHttpClientFactory) | WARN |
| async void | `async void` (excepto event handlers) | WARN |
| .Result | `.Result` o `.Wait()` en codigo async | WARN |
| Console.Write | `Console.Write` en proyectos no-consola | WARN |
| Thread.Sleep | `Thread.Sleep` (usar Task.Delay) | WARN |

- 0 violaciones: **PASS**
- 1+ violaciones: **WARN** con el conteo y detalle

---

## 5. Fase 4: Tests

Ejecutar:
```bash
dotnet test "$SLN" --no-build --verbosity normal 2>&1
```

- Si el exit code es distinto de 0: registrar **FAIL** con el numero de tests fallidos, y **DETENER** el pipeline.
- Si todos pasan: **PASS** con el conteo total de tests.
- Si no hay proyecto de tests: **WARN** "Sin tests detectados".

---

## 6. Fase 5: Escaneo de seguridad

Buscar en archivos `.cs`, `.json`, `.xml`, `.config` (excluyendo `obj/`, `bin/`):

| Patron | Buscar |
|--------|--------|
| Connection strings hardcoded | `Server=.*Password=` o `Data Source=.*pwd=` en .cs |
| API keys | `apikey`, `api_key`, `secret`, `password` asignados a string literal |
| Certificados en repo | archivos `.pfx`, `.key`, `.pem` en el repositorio |
| Tokens hardcoded | `Bearer ` seguido de string literal |

- 0 hallazgos: **PASS**
- 1+ hallazgos: **FAIL** con detalle (seguridad siempre es FAIL, no WARN)

---

## 7. Fase 6: Formato

Ejecutar:
```bash
dotnet format "$SLN" --verify-no-changes --verbosity diagnostic 2>&1
```

- Exit code 0: **PASS** (formato correcto)
- Exit code distinto de 0: **WARN** "Se requieren cambios de formato. Ejecutar `dotnet format`"
- Si `dotnet format` no esta disponible: **WARN** "dotnet format no disponible"

---

## 8. Fase 7: Revision del diff

Ejecutar:
```bash
git diff --stat
git diff --cached --stat
```

Analizar:
- Archivos con >500 lineas cambiadas: listarlos como **WARN**
- Archivos `.cs` nuevos sin test correspondiente (buscar `*Tests.cs`): listar como **WARN**
- Si no hay cambios: **PASS** "Sin cambios pendientes"
- Si hay cambios normales: **PASS** con resumen

---

## 9. Generar tabla resumen

Construir la tabla con los resultados de todas las fases ejecutadas:

```markdown
## Resultado Verificacion

| Fase | Estado | Detalle |
|------|--------|---------|
| 1. Build | {estado} | {detalle} |
| 2. Diagnosticos | {estado} | {detalle} |
| 3. Anti-patrones | {estado} | {detalle} |
| 4. Tests | {estado} | {detalle} |
| 5. Seguridad | {estado} | {detalle} |
| 6. Formato | {estado} | {detalle} |
| 7. Diff review | {estado} | {detalle} |

### Resultado global: {PASS|WARN|FAIL}
```

Iconos de estado:
- PASS: `✅ PASS`
- WARN: `⚠️ WARN`
- FAIL: `❌ FAIL`
- SKIP: `⏭️ SKIP` (si se detuvo por short-circuit)

Si el resultado global es **PASS**: "Listo para commit/PR"
Si el resultado global es **WARN**: "Proceder con precaucion. Revisar los avisos."
Si el resultado global es **FAIL**: "NO proceder. Corregir los errores antes de commit/PR."
