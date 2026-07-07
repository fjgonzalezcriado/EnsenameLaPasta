---
globs:
  - "**/*.cs"
---

# Regla: Estilo .NET — modernización de C# (IDE0290 y familia)

> **Portátil**: copia este archivo a `.claude/rules/dotnet-code-style.md` de cualquier proyecto .NET.
> Al ser una *rule* con `globs: **/*.cs`, Claude la carga **automáticamente en cada sesión** cuando
> trabaja con C#, sin invocarla. Su hook gemelo `.claude/hooks/dotnet-code-style-guard.ps1` avisa
> (WARN-first, no bloquea) al escribir/editar `.cs`. Objetivo: mantener el C# libre de las
> sugerencias de estilo `IDExxxx` de forma automática y verificable.

---

## Contexto

Las reglas `IDExxxx` son **sugerencias del analizador de estilo** (`Microsoft.CodeAnalysis.CSharp.CodeStyle`).
**No aparecen en `dotnet build`** por defecto — solo en el IDE (VS/Rider) y vía
`dotnet format style` o con `<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>`. Por eso hay que
detectarlas explícitamente, no basta con que el build esté verde.

## Conjunto cubierto por esta regla

Verificado en el catálogo oficial de Microsoft Learn (reglas de estilo de lenguaje). Se priorizan las
que se disparan en la práctica en proyectos .NET 8/10 modernos y son **mecánicas y seguras**:

| Regla | Qué marca | Antes → Después |
|---|---|---|
| **IDE0290** | Constructor que solo asigna params a campos | `public Foo(IBar b){_bar=b;}` → `class Foo(IBar b){ private readonly IBar _bar = b; }` |
| **IDE0300** | Inicializar array con expresión de colección | `new[] { 1, 2 }` → `[1, 2]` |
| **IDE0301** | Colección vacía con expresión de colección | `Array.Empty<T>()` / `new List<T>()` → `[]` |
| **IDE0305** | Fluent `.ToList()`/`.ToArray()` → expr. de colección | `items.ToList()` (en init) → `[.. items]` |
| **IDE0028** | Inicializador de colección → expr. de colección | `new List<int>() { 1, 2 }` → `[1, 2]` |
| **IDE0090** | `new` innecesario (target-typed) | `new Foo() ...` → `new() ...` |
| **IDE0063** | `using` con recurso → `using` simple | `using (var x = ...) { ... }` → `using var x = ...;` |
| **IDE0330** | Bloqueo con `object` → `System.Threading.Lock` | `private readonly object _gate = new();` + `lock` → `private readonly Lock _gate = new();` (net9+) |
| **IDE0042** | Deconstrucción de variable | `var t = M(); use t.a, t.b;` → `var (a, b) = M();` |
| **IDE0270** | `??` para null check | `if (x == null) x = ...;` → `x ??= ...;` |

> El hook detecta textualmente **IDE0290, IDE0300, IDE0028, IDE0063 e IDE0330** (las fiables por regex).
> Las demás (IDE0042 deconstrucción, IDE0305 fluida, IDE0090, IDE0270) las resuelve `dotnet format`,
> que es el árbitro final: si una regla no la ve el hook, `dotnet format style` sí la detecta.

## Cuándo actuar

1. **Al CREAR** código C#: escríbelo ya en el estilo moderno.
   - Clases con dependencias inyectadas (servicios, controllers, providers, hosted services, helpers
     de test) → **constructor primario conservando los campos `_field`**:

     ```csharp
     public sealed class FooService(IBar bar, TimeProvider time) : IFooService
     {
         private readonly IBar _bar = bar;
         private readonly TimeProvider _time = time;
         // el cuerpo sigue usando _bar / _time como siempre
     }
     ```
   - Arrays / listas literales → **expresión de colección** `[...]` en vez de `new[] { ... }`.
   - `using` de un recurso de vida = el bloque → **`using` simple** `using var x = ...;`.

2. **Antes de cerrar un evolutivo o commitear** C#: detecta las pendientes y arréglalas en bloque.

## Cómo detectar / arreglar (no requiere IDE)

```bash
# Detectar TODAS las reglas de estilo pendientes (lista, no cambia nada):
dotnet format style <solucion-o-proyecto> --severity info --verify-no-changes

# Arreglar UNA regla concreta (recomendado: de una en una, revisando el diff):
dotnet format style <solucion-o-proyecto> --diagnostics IDE0290 --severity info
dotnet format style <solucion-o-proyecto> --diagnostics IDE0300 --severity info
# ... etc.
```

- `dotnet format` es **mecánico** y **conserva los campos `_field`** en IDE0290
  (`class X(dep) { private readonly T _f = dep; }`) → cumple el analizador sin cambiar la convención
  de nombres ni el cuerpo de los métodos.
- **SIEMPRE** después: `dotnet build` + `dotnet test` → 0 errores y tests verdes.

## Reglas y límites

- **NO** conviertas a constructor primario clases cuyo constructor tenga **lógica** además de asignar
  (validaciones, cálculos, llamadas): IDE0290 no las marca; respétalas.
- **Mantén** la convención de campos del proyecto (`_camelCase`). El patrón
  `private readonly T _f = p;` satisface IDE0290 sin cambiar el estilo de acceso interno.
- **Entidades de dominio** con constructor privado + *factory* (p.ej. EF Core) **no aplican** a IDE0290.
- **IDE0300/0305/0028 no siempre las autoaplica `dotnet format`** en inicializadores de propiedad
  (`public IReadOnlyList<string> X { get; } = new[] { ... };`). Si `--verify-no-changes` las reporta
  pero el fix no las toca, cámbialas **a mano** a `= [...]`.
- **IDE0330** (`System.Threading.Lock`) requiere **.NET 9+**; en TFM anteriores no aplica.
- Si el proyecto **no** usa C# 12+ (`<LangVersion>` < 12 o TFM antiguo), IDE0290/0300/… no aplican:
  no toques nada.
- **No** ejecutes `dotnet format style` sin `--diagnostics` en un repo grande sin revisar: arrastra
  todos los cambios de estilo a la vez y dificulta el review. Filtra por regla.

## Alcance del hook gemelo

`.claude/hooks/dotnet-code-style-guard.ps1` (PreToolUse Write|Edit, **exit 1 = aviso, no bloquea**):
avisa cuando el `.cs` que vas a escribir contiene un patrón de IDE0290/0300/0028/0063/0330. Es un
recordatorio; el arreglo real y la cobertura completa la da `dotnet format style`.

---

*Regla condicional STIC.IA — estilo .NET. Autocontenida y portátil entre proyectos (regla + hook).*
