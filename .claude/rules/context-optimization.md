# Regla: Optimizacion de Contexto y Tokens

> Esta regla se aplica SIEMPRE. Claude debe seguir estas practicas para minimizar
> el consumo de tokens y mantener el contexto limpio durante sesiones de trabajo.

---

## Principio General

El contexto de Claude Code es grande pero finito:
- **Opus 4.6 / Sonnet 4.6**: 1M tokens (~940K efectivos tras overhead de sistema)
- **Haiku 4.5**: 200K tokens (~150K efectivos)

Cada archivo leido, cada salida de comando y cada mensaje consumen tokens.
Aunque 1M es mucho, una sesion intensa puede consumir 200-400K en 50-100 tool calls.
**Optimizar el contexto = sesiones mas largas y respuestas de mayor calidad.**

> Nota: Sonnet y Haiku reciben tracking de tokens (`Token usage: X/Y; Z remaining`).
> Opus no recibe este tracking explicito. Usar las metricas de referencia como guia.

---

## Reglas de Lectura de Archivos

### NUNCA releer archivos ya leidos

- Si ya leiste un archivo en esta sesion, NO lo releas a menos que el usuario lo haya modificado manualmente.
- Si necesitas una seccion especifica de un archivo grande, usa `offset` y `limit` en Read.
- Preferir `Grep` para buscar patrones antes de leer archivos completos.

### Leer solo lo necesario

- Archivos > 500 lineas: leer solo la seccion relevante con `offset`/`limit`.
- Nunca leer archivos binarios, imagenes o PDFs completos si solo necesitas metadatos.
- Para archivos de configuracion (JSON, YAML): leer solo si vas a modificarlos.

---

## Reglas de Busqueda

### Preferir herramientas especificas sobre Bash

| Tarea | Usar | NO usar |
|-------|------|---------|
| Buscar archivos por nombre | `Glob` | `find`, `ls -R` |
| Buscar texto en archivos | `Grep` | `grep`, `rg` via Bash |
| Leer un archivo | `Read` | `cat`, `head`, `tail` |
| Editar un archivo | `Edit` | `sed`, `awk` |

### Limitar resultados de busqueda

- Usar `head_limit` en Grep (default 250, reducir si solo necesitas confirmar existencia).
- Usar `output_mode: "files_with_matches"` cuando solo necesitas saber QUE archivos contienen algo, no las lineas.
- Usar `output_mode: "count"` cuando solo necesitas saber CUANTAS coincidencias hay.

---

## Reglas de Subagentes

### Delegar operaciones verbose a subagentes

Las siguientes operaciones deben ejecutarse en subagentes (Agent tool) para que su output
NO consuma el contexto principal:

- Busquedas abiertas que requieren multiples rondas de Grep/Glob
- Lectura de muchos archivos para recopilar informacion
- Ejecucion de tests con output extenso
- Analisis de logs o outputs grandes
- Operaciones de build con output verbose

### Como delegar correctamente

```
Agent(subagent_type="Explore", prompt="Busca todos los archivos que usan ILogger en src/")
```

El subagente devuelve un RESUMEN, no el output completo. Esto puede ahorrar miles de tokens.

---

## Reglas de Compactacion

### Cuando compactar proactivamente

Claude debe sugerir `/compact` al usuario en estas situaciones:

1. **Tras completar una tarea grande** (refactoring, migracion, analisis completo)
2. **Antes de iniciar una tarea nueva no relacionada** con lo anterior
3. **Cuando el contexto se siente "pesado"** (respuestas lentas, muchos archivos leidos)
4. **Tras N tool calls** sin compactar:
   - Opus 4.6 / Sonnet 4.6 (1M): **80+ tool calls**
   - Haiku 4.5 (200K): **30+ tool calls**
5. **Tras N archivos completos** leidos:
   - Opus/Sonnet (1M): **25+ archivos**
   - Haiku (200K): **10+ archivos**

### Como sugerir compactacion

No interrumpir el flujo del usuario. Sugerir al FINAL de una respuesta:

```
[Tarea completada]

💡 Sesion extensa — recomiendo `/compact` antes de continuar con otra tarea.
```

### Que preservar al compactar

Si el usuario ejecuta `/compact`, Claude debe asegurar que el resumen de compactacion incluya:
- Estado actual del evolutivo activo
- Archivos modificados en esta sesion
- Decisiones tomadas
- Proximos pasos pendientes

---

## Reglas de Output

### Minimizar output de Bash

- Redirigir output largo: `| head -20` o `| tail -10`
- Para builds: `dotnet build -v quiet` o `--verbosity minimal`
- Para tests: `dotnet test --verbosity minimal --no-build`
- Para git: `git status --short` (no `-v`), `git log --oneline -5`

### No generar output innecesario

- No usar `echo` para confirmar operaciones — la herramienta ya retorna el resultado.
- No listar directorios completos si solo necesitas verificar un archivo.
- Preferir `test -f archivo && echo "exists"` sobre `ls -la directorio/`.

---

## Anti-patrones de Consumo de Tokens

### EVITAR estos patrones

| Anti-patron | Alternativa |
|-------------|-------------|
| Leer un archivo completo para buscar una linea | `Grep` con patron especifico |
| `git diff` sin limitar (puede ser enorme) | `git diff --stat` primero, luego archivo especifico |
| `dotnet build` con output completo | `dotnet build -v quiet` |
| Releer CLAUDE.md en cada respuesta | Ya esta en contexto automaticamente |
| Leer todos los archivos de un directorio | `Glob` + `Grep` para filtrar primero |
| `git log` sin limite | `git log --oneline -10` |
| Ejecutar multiples Grep secuenciales | Un solo Grep con regex combinado |

---

## Metricas de Referencia

### Modelos con 1M tokens (Opus 4.6, Sonnet 4.6)

| Metrica | Umbral para actuar |
|---------|---------------------|
| Tool calls en sesion | > 80 → sugerir /compact |
| Archivos leidos completos | > 25 → sugerir /compact |
| Errores de build consecutivos | > 3 → detenerse y replantear |
| Tareas completadas | > 4 no relacionadas → sugerir /compact |

### Modelos con 200K tokens (Haiku 4.5)

| Metrica | Umbral para actuar |
|---------|---------------------|
| Tool calls en sesion | > 30 → sugerir /compact |
| Archivos leidos completos | > 10 → sugerir /compact |
| Errores de build consecutivos | > 3 → detenerse y replantear |
| Tareas completadas | > 2 no relacionadas → sugerir /compact |

---

*Regla condicional v3.8.2 - Optimizacion de contexto*
