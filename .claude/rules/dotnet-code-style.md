---
globs:
  - "**/*.cs"
---

# Regla: Estilo .NET — constructores primarios (IDE0290)

> **Portátil**: copia este archivo a `.claude/rules/dotnet-code-style.md` de cualquier proyecto .NET.
> Al ser una *rule* con `globs: **/*.cs`, Claude la carga **automáticamente en cada sesión** cuando
> trabaja con C#, sin que haya que invocarla. Objetivo: mantener el código libre de la advertencia
> **IDE0290** (usar constructor primario) de forma automática y verificable.

---

## Qué es IDE0290

"Usar constructor primario": una clase cuyo constructor **solo asigna parámetros a campos** puede
escribirse como constructor primario (C# 12+). **No aparece en `dotnet build`** (es una sugerencia
del analizador de estilo), sí en el IDE (VS/Rider) y con `dotnet format` / `EnforceCodeStyleInBuild`.

## Cuándo actuar

1. **Al CREAR** una clase nueva con dependencias inyectadas (servicios, controllers, providers,
   hosted services, helpers de test): escríbela **ya** como constructor primario, **conservando los
   campos `_field`** (convención de campos privados del proyecto):

   ```csharp
   public sealed class FooService(IBar bar, TimeProvider time) : IFooService
   {
       private readonly IBar _bar = bar;
       private readonly TimeProvider _time = time;
       // ... el cuerpo sigue usando _bar / _time como siempre
   }
   ```

2. **Antes de cerrar un evolutivo o de commitear** cambios de C#, si sospechas que quedan IDE0290
   pendientes (código heredado, o generado con el patrón antiguo), detéctalos y arréglalos en bloque.

## Cómo detectar / arreglar (no requiere IDE)

```bash
# Detectar (lista, no cambia nada):
dotnet format style <solucion-o-proyecto> --diagnostics IDE0290 --severity info --verify-no-changes

# Arreglar automáticamente:
dotnet format style <solucion-o-proyecto> --diagnostics IDE0290 --severity info
```

- `dotnet format` convierte a constructor primario **conservando los campos `_field`**
  (`class X(dep) { private readonly T _f = dep; }`) → cumple el analizador **sin** cambiar la
  convención de nombres ni el cuerpo de los métodos. Es un cambio **mecánico**.
- **SIEMPRE** después: `dotnet build` + `dotnet test` para confirmar 0 errores y tests verdes.

## Reglas y límites

- **NO** conviertas clases cuyo constructor tenga **lógica** además de asignar (validaciones,
  cálculos, llamadas): IDE0290 no las marca; respétalas.
- **Mantén** la convención de campos del proyecto (`_camelCase`). El patrón
  `private readonly T _f = p;` satisface IDE0290 sin cambiar el estilo de acceso interno.
- **Entidades de dominio** con constructor privado + *factory* (p.ej. EF Core) **no aplican**.
- Si el proyecto **no** usa C# 12+ (`<LangVersion>` &lt; 12 o TFM antiguo), IDE0290 no aplica:
  no toques nada.
- No conviertas por conversión: solo cuando el analizador lo marca (evita falsos positivos en
  clases con varios constructores o inicializadores complejos).

## Alcance

Esta regla cubre **solo IDE0290** (probado seguro y mecánico). Para ampliar a otras sugerencias de
estilo (p.ej. `IDE0028` inicializadores de colección, `IDE0300` expresiones de colección), añade el
id al parámetro `--diagnostics` **de una en una**, revisando el diff y con build+tests verdes tras
cada una — no apliques `dotnet format style` sin filtrar (arrastraría cambios no deseados).

---

*Regla condicional STIC.IA — estilo .NET. Autocontenida y portátil entre proyectos.*
