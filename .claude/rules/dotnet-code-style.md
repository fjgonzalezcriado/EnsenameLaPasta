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

## Reglas de RENDIMIENTO (analizadores CA)

Además del estilo, esta regla cubre las **reglas de rendimiento** del análisis de código
(`CAxxxx`, categoría *Performance*). A diferencia de las `IDExxxx`, las `CA` son **analizadores**
que **sí pueden aparecer en `dotnet build`** (según `<AnalysisLevel>`/severidad); si el proyecto
tiene `TreatWarningsAsErrors=true` y compila limpio, es que están a nivel *info/suggestion* — hay
que detectarlas explícitamente. Selección de las que se disparan en la práctica y aportan valor:

| Regla | Qué marca | Preferir |
|---|---|---|
| **CA1827/CA1829** | `.Count() == 0 / > 0`; `.Count()` sobre algo con `.Count`/`.Length` | `!Any()` / `Any()`; propiedad `.Count`/`.Length` |
| **CA1820** | Comparar cadena con `""` | `string.IsNullOrEmpty(s)` o `s.Length == 0` |
| **CA1834** | `StringBuilder.Append("x")` de 1 carácter | `Append('x')` (literal char) |
| **CA1848 / CA1873** | Logging con interpolación `$"..."` / argumentos costosos (boxing) | Plantilla estructurada `"{Campo}"` + args; en hot paths `LoggerMessage` o guarda `IsEnabled` |
| **CA1859** | Tipo declarado por interfaz donde vale el concreto | Tipo **concreto** en campos/retornos **privados** (devirtualización) |
| **CA1861** | Array constante como argumento en llamada repetida | Campo `static readonly` (idealmente `= [...]`) |
| **CA1822** | Miembro que no usa estado de instancia | Marcar `static` |
| **CA1860** | `.Any()` cuando existe `.Count`/`.Length` | Propiedad `.Count > 0` |
| **CA1862** | `.ToLower()`/`.ToUpper()` para comparar | Sobrecarga `StringComparison.OrdinalIgnoreCase` |

### Cómo detectar / arreglar

```bash
# Detectar TODAS las CA de rendimiento a nivel info (no cambia nada):
dotnet format analyzers <sln> --severity info --verify-no-changes
# Arreglar una con fixer (CA1861, CA1822 lo tienen; CA1859 NO -> a mano):
dotnet format analyzers <sln> --diagnostics CA1861 --severity info
```

- **CA1859** (tipo concreto): solo en miembros **privados** (campos, retornos de helpers). No cambiar
  la firma pública de una interfaz por un concreto — pierde el contrato de inmutabilidad.
- **CA1861** (array→`static readonly`): el auto-fixer genera nombres pésimos (`collection`,
  `collection0`); **renómbralos** a descriptivos PascalCase y pásalos a `= [...]`.
- **CA1873 es de criterio**: guardar cada `_logger.LogXxx(...)` con `if (_logger.IsEnabled(...))`
  para evitar el boxing de tipos valor **solo compensa en hot paths**. En logging poco frecuente
  (arranque, errores, poll lento) el coste es despreciable y el guard añade ruido → **no lo apliques
  por sistema**; el arreglo correcto de alto rendimiento es `LoggerMessage` (CA1848), reservado a
  rutas calientes. Documenta la decisión si lo dejas sin aplicar.

## Comprobación de barrido a cero (checklist pre-commit / cierre de evolutivo)

Ni `IDExxxx` ni las `CAxxxx` a nivel *info* aparecen en `dotnet build` — un build verde **no**
garantiza que estén a cero. Antes de commitear C# o cerrar un evolutivo, ejecuta este barrido
(sustituye `<sln>` por la solución/proyecto, p. ej. `MiApp.slnx`):

```bash
# 1) ESTILO (IDExxxx) — no cambia nada, solo lista lo pendiente:
dotnet format style     <sln> --severity info --verify-no-changes

# 2) RENDIMIENTO (CAxxxx) — idem:
dotnet format analyzers <sln> --severity info --verify-no-changes
```

- **Ambos comandos sin salida ⇒ 0 sugerencias** (objetivo). Si listan algo, arréglalo con el mismo
  comando sin `--verify-no-changes` y `--diagnostics <ID>` (una regla a la vez, revisando el diff),
  o a mano cuando no haya *fixer* (CA1859, inicializadores de propiedad IDE0300/0305, etc.).
- **Recuento rápido** de lo que queda, agrupado por regla:

  ```bash
  dotnet format style     <sln> --severity info --verify-no-changes 2>&1 | grep -oE 'IDE[0-9]{4}' | sort | uniq -c
  dotnet format analyzers <sln> --severity info --verify-no-changes 2>&1 | grep -oE 'CA[0-9]{4}'  | sort | uniq -c
  ```
- **Cierre**: tras arreglar, `dotnet build` (0/0) + `dotnet test` (verde) antes de commitear.
- El hook avisa *al escribir*; este barrido es la **red de seguridad** que confirma el cero global.

## Alcance del hook gemelo

`.claude/hooks/dotnet-code-style-guard.ps1` (PreToolUse Write|Edit, **exit 1 = aviso, no bloquea**):
avisa cuando el `.cs` que vas a escribir contiene un patrón de **IDE0290/0300/0028/0063/0330** o de
rendimiento textual **CA1827/CA1820/CA1834/CA1848**. Es un recordatorio; las CA que exigen análisis
semántico (**CA1859** tipo concreto, **CA1861** array constante, **CA1822** miembro estático,
**CA1873** boxing en logging estructurado) las detecta `dotnet format analyzers`, no el hook.

---

*Regla condicional STIC.IA — estilo y rendimiento .NET. Autocontenida y portátil entre proyectos (regla + hook).*
