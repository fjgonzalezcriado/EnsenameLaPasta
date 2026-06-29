Pipeline de limpieza sistematica de 7 pasos para codigo .NET con deteccion de slop IA

# /clean - Pipeline de Limpieza Codigo .NET

> Inspirado en **de-sloppify** (dotnet-claude-kit) y **slopwatch** (Aaronontheweb).
> Ejecuta 7 pasos en orden (formato primero, codigo muerto ultimo) + escaneo slop IA.

---

## REGLAS CRITICAS

> **REGLA 1**: Verificar build+test entre los pasos 3, 4, 6 y 7. Si algo se rompe, revertir el paso.
> **REGLA 2**: Cada paso que modifique archivos sugiere un commit separado (el usuario decide).
> **REGLA 3**: Paso 4 (dead code) SIEMPRE pregunta antes de eliminar. NUNCA borrar automaticamente.
> **REGLA 4**: Paso 5 (TODOs) es solo reporte. No eliminar ni modificar comentarios.
> **REGLA 5**: Usar MCP Roslyn cuando este disponible; fallback a CLI/grep si no.

---

## PASO 0: Preparacion

1. Localizar la solucion (.sln/.slnx). Si hay multiples, preguntar cual usar.
2. Ejecutar `dotnet build` para verificar que el proyecto compila antes de empezar.
3. Ejecutar `dotnet test` para capturar baseline de tests (si hay proyecto de tests).
4. Si build o tests fallan, DETENERSE e informar al usuario.

---

## PASO 1: Format (dotnet format)

Ejecutar formateo automatico del codigo C#:

```bash
dotnet format <solucion> --verbosity diagnostic
```

Registrar cuantos archivos se modificaron. Si hay cambios, sugerir commit:

```
style: auto-format C# files
```

---

## PASO 2: Usings (IDE0005)

Eliminar directivas `using` no utilizadas:

```bash
dotnet format <solucion> --diagnostics IDE0005 --severity info
```

Registrar cuantos usings se eliminaron. Si hay cambios, sugerir commit:

```
refactor: remove unused usings
```

---

## PASO 3: Analyzers (warnings como errores)

Compilar tratando warnings como errores para detectar problemas de analizadores:

```bash
dotnet build <solucion> -warnaserror
```

Si el MCP Roslyn esta disponible, usar `get_diagnostics` para obtener la lista completa.

Para cada warning:
1. Evaluar si es un falso positivo o un problema real.
2. Corregir los problemas reales (nullable, async sin await, variables sin usar, etc.).
3. Para warnings que no se pueden corregir, documentar la justificacion.

Tras las correcciones, verificar: `dotnet build` + `dotnet test`.

Si hay cambios, sugerir commit:

```
fix: resolve analyzer warnings
```

---

## PASO 4: Dead Code (codigo muerto)

Buscar codigo que no se usa en ninguna parte del proyecto.

**Con MCP Roslyn** (preferido):
- Usar `find_dead_code` para detectar clases, metodos y propiedades sin referencias.

**Sin MCP** (fallback manual):
- Buscar clases `internal`/`private` sin referencias.
- Buscar metodos privados no invocados.
- Buscar variables asignadas pero nunca leidas.

**Comprobaciones de seguridad** — NO marcar como dead code si:
- Es `public` en un proyecto de libreria (API publica).
- Tiene atributos de reflexion (`[JsonProperty]`, `[Column]`, `[Bind]`, etc.).
- Esta registrado en DI (`services.AddScoped<T>`, `services.AddTransient<T>`, etc.).
- Tiene `[Obsolete]` con fecha futura o plan de migracion documentado.
- Es un handler de eventos, middleware, o filtro registrado por convencion.
- Es usado via reflexion (`typeof(T)`, `nameof(T)`, `Activator.CreateInstance`).

**IMPORTANTE**: Presentar la lista al usuario y PREGUNTAR que eliminar. No borrar nada automaticamente.

Tras las eliminaciones aprobadas, verificar: `dotnet build` + `dotnet test`.

Si hay cambios, sugerir commit:

```
refactor: remove dead code
```

---

## PASO 5: TODOs y Deuda Tecnica (solo reporte)

Buscar todos los comentarios de deuda tecnica en el codigo:

```
// TODO:
// HACK:
// FIXME:
// BUG:
// UNDONE:
// XXX:
```

Para cada hallazgo, registrar: archivo, linea, texto del comentario.

**NO modificar ni eliminar nada.** Solo reportar para decision del usuario.

---

## PASO 6: Sealed Classes (optimizacion JIT)

Buscar clases que deberian ser `sealed` para optimizacion del JIT:
- Clase no es `abstract`.
- Clase no es `static`.
- Clase no es ya `sealed`.
- Clase no tiene clases derivadas en la solucion.

**Con MCP Roslyn** (preferido):
- Usar `find_implementations` para verificar que no hay clases derivadas.

**Sin MCP** (fallback):
- Buscar patrones `: NombreClase` en toda la solucion.

Excluir de sellado:
- Clases base de dominio (Entity, ValueObject, AuditableEntity).
- Clases que implementan patrones de herencia por diseno (Template Method, etc.).
- Clases con `virtual` members que sugieren intencion de herencia.

Aplicar `sealed` a las clases candidatas.

Tras los cambios, verificar: `dotnet build` + `dotnet test`.

Si hay cambios, sugerir commit:

```
perf: seal non-inherited classes
```

---

## PASO 7: CancellationToken en metodos async

Buscar metodos `public async` que NO reciben `CancellationToken` como parametro.

**Con MCP Roslyn** (preferido):
- Usar `detect_antipatterns` con patron AP009 si esta disponible.

**Sin MCP** (fallback):
- Buscar `public async Task` y `public async Task<` sin `CancellationToken` en la firma.

Excluir:
- Metodos de test (`[Fact]`, `[Theory]`, `[Test]`).
- Event handlers (`async void` — reportar como warning separado).
- Metodos `Main` o `Program`.
- Overrides de framework donde la firma es fija.

Anadir `CancellationToken ct = default` como ultimo parametro y propagarlo a las llamadas internas.

Tras los cambios, verificar: `dotnet build` + `dotnet test`.

Si hay cambios, sugerir commit:

```
refactor: add CancellationToken to async methods
```

---

## DETECCION SLOP (slopwatch)

Tras completar los 7 pasos, ejecutar un escaneo de patrones tipicos de codigo generado por IA:

| Regla | Patron | Busqueda |
|-------|--------|----------|
| **SW001** | Tests deshabilitados | `[Fact(Skip=` , `[Theory(Skip=` , `[Ignore]` , `[Ignore(` |
| **SW002** | Warnings suprimidos | `#pragma warning disable` sin `#pragma warning restore` en el mismo archivo |
| **SW003** | Catch vacios | `catch {` o `catch (` seguido de bloque vacio `{ }` sin logging |
| **SW004** | Sleep/Delay arbitrarios | `Thread.Sleep(` o `Task.Delay(` en codigo que NO es de test |
| **SW005** | Bypass CPM | `<PackageReference` con atributo `Version=` cuando existe `Directory.Packages.props` en la solucion |

Para cada regla, reportar: archivo, linea, contexto.

---

## FORMATO DE SALIDA

Al finalizar, presentar el resumen completo:

```markdown
## Resultado Limpieza

| Paso | Accion | Resultado |
|------|--------|-----------|
| 1. Format | dotnet format | X archivos formateados |
| 2. Usings | IDE0005 | X usings eliminados |
| 3. Analyzers | -warnaserror | X warnings corregidos |
| 4. Dead code | Analisis referencias | X elementos eliminados (con aprobacion) |
| 5. TODOs | Reporte | X comentarios encontrados |
| 6. Sealed | Clases selladas | X clases selladas |
| 7. CancellationToken | Async methods | X metodos actualizados |

### Deteccion Slop (slopwatch)

| Regla | Descripcion | Resultado |
|-------|-------------|-----------|
| SW001 | Tests deshabilitados | X encontrados |
| SW002 | Warnings suprimidos sin restore | X encontrados |
| SW003 | Catch vacios | X encontrados |
| SW004 | Sleep/Delay en produccion | X encontrados |
| SW005 | Bypass CPM (Version= con D.P.props) | X encontrados |

### Commits sugeridos
- [ ] `style: auto-format C# files`
- [ ] `refactor: remove unused usings`
- [ ] `fix: resolve analyzer warnings`
- [ ] `refactor: remove dead code`
- [ ] `perf: seal non-inherited classes`
- [ ] `refactor: add CancellationToken to async methods`
```

Si algun paso no produjo cambios, indicar "Sin cambios" en la columna Resultado.
Si algun paso se salto por error, indicar "Saltado: [razon]".
