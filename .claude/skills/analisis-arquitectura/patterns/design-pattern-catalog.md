> Skill: analisis-arquitectura | Version: 3.7.0

# Catalogo de Patrones de Diseno para Analisis Arquitectonico

Guia de referencia para detectar patrones de diseno (correctos y anti-patrones) durante el analisis arquitectonico.

---

## Patrones a Buscar

### Patrones Estructurales

| Patron | Donde buscar | Indicadores de correcta implementacion | Anti-patron comun |
|--------|-------------|---------------------------------------|-------------------|
| **Repository** | Clases `*Repository`, interfaces `I*Repository` | Interfaz en Domain/Application, implementacion en Infrastructure | Repositorio generico sin value, DbContext directo en services |
| **Unit of Work** | `IUnitOfWork`, `SaveChangesAsync` centralizado | Una unica llamada a SaveChanges por operacion de negocio | Multiples SaveChanges dispersos, transacciones implicitas |
| **Factory** | Metodos `Create()`, `Build()` en entidades o clases Factory | Encapsula logica de creacion compleja, validacion en factory | Constructor publico sin validacion, `new()` disperso |
| **Facade** | Clases `*Service` que orquestan multiples dependencias | Simplifica interfaz compleja, no contiene logica de negocio | God class que hace todo, service con 10+ dependencias |
| **Adapter** | Clases que envuelven APIs externas | Traduce interfaz externa a interna, desacopla | Uso directo de SDK externo en logica de negocio |

### Patrones de Comportamiento

| Patron | Donde buscar | Indicadores | Anti-patron |
|--------|-------------|-------------|-------------|
| **Strategy** | Interfaces con multiples implementaciones, DI condicional | Comportamiento intercambiable, Open/Closed principle | switch/if largo en metodo, hardcoded decisions |
| **Observer/Event** | `IDomainEvent`, `INotificationHandler`, EventHandlers | Desacoplamiento entre productor y consumidor | Llamadas directas entre modulos, acoplamiento temporal |
| **Template Method** | Clases base abstractas con metodos template | Hook methods para extensibilidad | Override completo de metodo base, copy-paste entre clases |
| **Mediator** | `IMediator`, `IRequest`, `IRequestHandler` | Desacoplamiento entre sender y handler | ServiceLocator, dependencia directa entre controllers y services |

### Patrones CQRS/DDD

| Patron | Donde buscar | Indicadores | Anti-patron |
|--------|-------------|-------------|-------------|
| **CQRS** | Commands vs Queries separados, `ICommand`/`IQuery` | Lectura optimizada (AsNoTracking), escritura con validacion | Mismo modelo para lectura y escritura, queries que mutan estado |
| **Specification** | `ISpecification<T>`, clases `*Spec` | Encapsula criterios de consulta, composable | WHERE clauses duplicadas en multiples repositorios |
| **Domain Event** | Eventos con `OccurredOn`, handlers reactivos | Comunicacion entre aggregates, side effects | Llamadas directas entre aggregates, logica en SaveChanges |
| **Value Object** | Records inmutables con `GetEqualityComponents` | Igualdad por valor, validacion en constructor, sin ID | Primitive obsession (string para Email, int para Money) |

### Patrones de Infraestructura

| Patron | Donde buscar | Indicadores | Anti-patron |
|--------|-------------|-------------|-------------|
| **Options Pattern** | `IOptions<T>`, `IOptionsMonitor<T>`, clases `*Settings` | Configuracion tipada y validada | `ConfigurationManager` directo, magic strings en config |
| **Decorator** | Pipeline behaviors, middleware, delegating handlers | Funcionalidad anadida sin modificar original | Herencia profunda, modificacion de clase base |
| **Singleton** | `AddSingleton<>()`, static classes con estado | Recurso compartido thread-safe, lifetime explicito | Static mutable, global state, dificil de testear |

---

## Anti-Patrones Criticos

### God Class
- **Deteccion**: Clase con >500 lineas, >10 dependencias inyectadas, >20 metodos publicos
- **Impacto**: Imposible de testear, viola SRP, atrae cambios constantes
- **Solucion**: Descomponer por responsabilidad, extraer a servicios especificos

### Service Locator
- **Deteccion**: `IServiceProvider.GetService<T>()` fuera de composition root, `HttpContext.RequestServices`
- **Impacto**: Dependencias ocultas, dificil de testear, viola DIP
- **Solucion**: Constructor injection, registrar dependencias en DI

### Tight Coupling
- **Deteccion**: `new ConcreteService()` en lugar de interfaz, referencias directas entre capas
- **Impacto**: Imposible mockear en tests, cambios en cascada
- **Solucion**: Extraer interfaz, inyectar via constructor

### Circular Dependencies
- **Deteccion**: A referencia B, B referencia A (en .csproj ProjectReference)
- **Impacto**: Compilacion fragil, diseno confuso, imposible de desacoplar
- **Solucion**: Extraer interfaz comun a un tercer proyecto, invertir dependencia

### Anemic Domain Model
- **Deteccion**: Entidades con solo propiedades publicas (getters/setters), toda la logica en servicios
- **Impacto**: Logica de negocio dispersa, reglas duplicadas, dificil de mantener
- **Solucion**: Mover logica a entidades, setters privados, factory methods

### Magic Strings/Numbers
- **Deteccion**: Strings literales en logica (`if (status == "active")`), numeros sin nombre
- **Impacto**: Errores silenciosos por typos, dificil de refactorizar
- **Solucion**: Constantes, enums, configuration

### Primitive Obsession
- **Deteccion**: `string email`, `decimal importe`, `int edad` en lugar de tipos especificos
- **Impacto**: Validacion dispersa, semantica perdida, errores de tipo
- **Solucion**: Value Objects (`Email`, `Dinero`, `Edad`)

---

## Formato de Reporte

Para cada patron detectado en el analisis, documentar:

```markdown
### [Nombre del Patron]

| Campo | Detalle |
|-------|---------|
| **Tipo** | Patron / Anti-patron |
| **Ubicacion** | Proyecto, namespace, clase |
| **Implementacion** | Correcta / Incompleta / Incorrecta |
| **Archivo ejemplo** | `ruta/al/archivo.cs:linea` |
| **Observaciones** | Detalles especificos |
```

---

## Busqueda Automatica

Comandos para detectar patrones en el codigo:

```bash
# Repository pattern
grep -r "IRepository\|IXxxRepository" src/ --include="*.cs"

# CQRS
grep -r "IRequest\|ICommand\|IQuery\|IRequestHandler" src/ --include="*.cs"

# Domain Events
grep -r "IDomainEvent\|DomainEvent\|AddDomainEvent" src/ --include="*.cs"

# Service Locator (anti-patron)
grep -r "GetService\|GetRequiredService\|RequestServices" src/ --include="*.cs"

# God Classes (>500 lineas)
find src/ -name "*.cs" -exec sh -c 'lines=$(wc -l < "$1"); [ "$lines" -gt 500 ] && echo "$1: $lines lines"' _ {} \;

# Magic Strings
grep -rn '"active"\|"pending"\|"completed"\|"error"' src/ --include="*.cs"

# Tight Coupling (new Service())
grep -rn "new.*Service\|new.*Repository\|new.*Handler" src/ --include="*.cs"
```

---

*Catalogo v3.7.0 - Patrones de diseno para analisis arquitectonico*
