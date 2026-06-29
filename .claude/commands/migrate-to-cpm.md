# /migrate-to-cpm - Migrar a Central Package Management

Wrapper ergonomico que invoca al agente `cpm-migration-assistant` para migrar la solucion .NET actual a CPM (Central Package Management con `Directory.Packages.props`).

**Origen**: item J5 bloque J, ADR-034.

---

## Uso

```
/migrate-to-cpm                       # Auditoria + plan + migracion (con confirmacion)
/migrate-to-cpm --dry-run             # Solo auditoria + plan (no modifica archivos)
/migrate-to-cpm --solution <path>     # Especificar solucion (proyectos multi-sln)
```

---

## Instrucciones para Claude

Al ejecutar `/migrate-to-cpm`, Claude debe:

1. **Verificar pre-requisitos**:
   - Existe al menos un `.csproj` en `03_Desarrollo/` o subcarpetas
   - El proyecto compila ANTES de empezar (`dotnet build` exit 0). Si no, abortar.
   - Si `Directory.Packages.props` ya existe: mostrar mensaje + sugerir invocar `cpm-migration-assistant` en modo auditoria (no migracion)

2. **Detectar contexto**:
   - Leer `_duran/ESTADO_PROYECTO.json.soluciones`
   - Si `multiSolucion: true` y NO se paso `--solution`: preguntar al usuario que solucion migrar (listar las disponibles)
   - Si `multiSolucion: false`: usar la solucion unica

3. **Invocar al agente**:
   ```
   Task tool con:
     subagent_type: cpm-migration-assistant
     prompt: "Migrar a CPM la solucion {path}. Modo: {dry-run|full}.
              Reportar conflictos detectados antes de modificar csprojs."
   ```

4. **Si NO es `--dry-run`**, tras el agente devolver auditoria:
   - Mostrar al usuario:
     - Numero de csprojs a modificar
     - Conflictos detectados (estrategia propuesta por cada uno)
     - Lista de paquetes a consolidar
   - **Pedir confirmacion explicita**: "Aplicar la migracion? S/N"
   - Si S: continuar con FASE 4-6 del agente
   - Si N: parar, dejar el plan en _duran/ para revision

5. **Post-migracion** (FASE 6-7 del agente):
   - Ejecutar `dotnet restore` + `dotnet build`
   - Si build OK: sugerir commit con mensaje `chore(cpm): migrar a Central Package Management`
   - Si build FAIL: NO commitear, mostrar errores y delegar a `build-fixer` agent

---

## Anti-patrones

- **NO ejecutar si el proyecto no compila** — la migracion CPM presupone codigo en buen estado
- **NO ejecutar en pleno desarrollo de feature** — preferir despues de cerrar el evolutivo activo
- **NO hacer commit automatico** — siempre dejar al usuario revisar el diff

---

## Casos comunes de uso

### A. Proyecto legacy sin CPM (caso Intercambio)

```
Usuario: "/migrate-to-cpm --dry-run"
Claude:
  1. Detecta 19 csprojs sin Directory.Packages.props
  2. Audita versiones (encuentra 3 conflictos: Polly, Newtonsoft.Json, EF Core)
  3. Genera plan con resoluciones propuestas
  4. NO modifica nada (dry-run)
  5. Sugiere review del plan y luego /migrate-to-cpm sin flag
```

### B. Multi-solucion

```
Usuario: "/migrate-to-cpm --solution 03_Desarrollo/MiApp.sln"
Claude:
  1. Solo procesa csprojs referenciados en MiApp.sln
  2. Resto del repo (otras .sln) sin tocar
```

### C. CPM ya habilitado (auditoria)

```
Usuario: "/migrate-to-cpm"
Claude detecta Directory.Packages.props existente:
  "CPM ya esta habilitado en {path}. Sugerencias:
   - Para AUDITAR coherencia: invocar nuget-analyzer agent
   - Para ACTUALIZAR versiones: invocar nuget-analyzer con --update"
```

---

## Integracion con otros comandos/agents

- Tras `/migrate-to-cpm` exitoso: invocar `nuget-analyzer` para auditar versiones consolidadas (vulnerabilidades, deprecated)
- Si build falla post-migracion: invocar `build-fixer` agent
- Tras 1 mes con CPM: invocar `architecture-validator` para confirmar coherencia de capas (es mas facil con CPM)

---

*Comando plantilla STIC.IA - item J5 bloque J (ADR-034). Wrapper del agente cpm-migration-assistant.*
