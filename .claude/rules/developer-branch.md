---
globs: []
---

# Regla: Estrategia Developer Branch

> Esta regla se activa cuando `_duran/ESTADO_PROYECTO.json` tiene `configuracion.branching.estrategia == "developer-branch"`.
> Define el flujo de trabajo con rama personal remota y features locales.

---

## Deteccion

Claude debe verificar `configuracion.branching.estrategia` en ESTADO_PROYECTO.json al inicio de cualquier tarea de desarrollo. Si el valor es `"developer-branch"`, aplicar las reglas de este archivo.

---

## Configuracion Esperada

```json
"branching": {
    "estrategia": "developer-branch",
    "ramaBase": "dev.{usuario}",
    "mergeStrategy": "merge",
    "convencionRamas": "yyyyMMdd-{tipo}-{codigo}-{descripcion}",
    "pushFeatures": false,
    "tiposTarea": {
        "DT": "Deuda técnica",
        "HV": "Evolutivo"
    }
}
```

---

## Flujo Obligatorio

Antes de escribir cualquier cambio de codigo, seguir este flujo:

### 1. Posicionarse en la rama base

```bash
# Leer ramaBase de ESTADO_PROYECTO.json (ej: dev.claudio)
git checkout {ramaBase}
git pull origin {ramaBase}
```

### 2. Crear rama local

```bash
# Formato: yyyyMMdd-{tipo}-{codigo}-{descripcion}
# La fecha es la fecha de INICIO de la tarea (hoy)
git checkout -b {yyyyMMdd}-{tipo}-{codigo}-{descripcion}

# Ejemplos:
# git checkout -b 20260406-DT-005-fix-xyz
# git checkout -b 20260406-HV-18-filtro-becas
```

**Tipo de tarea**: Leer `tiposTarea` de ESTADO_PROYECTO.json. Si hay mas de un tipo disponible, preguntar al usuario cual aplica.

### 3. Desarrollar

- Commits parciales permitidos (atomicos, conventional commits)
- Compilar y verificar: `dotnet build` / `dotnet test`

### 4. Merge a rama base

> **CONFIRMAR con el usuario antes de ejecutar**

```bash
git checkout {ramaBase}
git merge {rama-feature}
```

### 5. Push al remoto

> **CONFIRMAR con el usuario antes de ejecutar**

```bash
git push origin {ramaBase}
```

### 6. Limpieza (opcional)

```bash
# La rama local se puede mantener como historico o borrar
git branch -d {rama-feature}
```

---

## Restricciones CRITICAS

| Regla | Descripcion |
|-------|-------------|
| **NUNCA** pushear ramas locales | Las ramas feature solo existen localmente |
| **NUNCA** desarrollar en rama base | Siempre crear rama local primero |
| **SIEMPRE** confirmar merge/push | Pasos 4 y 5 requieren aprobacion del usuario |
| **SIEMPRE** usar fecha de inicio | La fecha en el nombre es cuando se INICIO la tarea |
| **SIEMPRE** leer config | Rama base, tipos y nomenclatura vienen de ESTADO_PROYECTO.json |

---

## Nomenclatura de Ramas

El formato se lee de `configuracion.branching.convencionRamas`:

```
yyyyMMdd-{tipo}-{codigo}-{descripcion}
```

| Componente | Fuente | Ejemplo |
|-----------|--------|---------|
| `yyyyMMdd` | Fecha actual (inicio tarea) | `20260406` |
| `{tipo}` | De `tiposTarea` en config | `DT`, `HV`, `BUG` |
| `{codigo}` | Codigo evolutivo o numero secuencial | `005`, `18` |
| `{descripcion}` | Descripcion breve kebab-case | `fix-xyz`, `filtro-becas` |

---

## Diferencias con Otras Estrategias

| Aspecto | developer-branch | github-flow-simplificado |
|---------|-----------------|--------------------------|
| Rama base | `dev.{usuario}` (personal) | `main` |
| Features se pushean | No | No |
| Merge | plain merge | `--no-ff` |
| Nomenclatura | Temporal (fecha) | Semantica (feature/...) |
| Tipo tarea en rama | Si (DT/HV/...) | No |

---

## Cuando Recomendar

- Desarrollador unico sin CI/CD formal
- Proyectos con rama personal remota como punto de integracion
- Equipos que prefieren trazabilidad temporal en ramas

---

*Regla condicional v3.7.0 - Estrategia Developer Branch*
