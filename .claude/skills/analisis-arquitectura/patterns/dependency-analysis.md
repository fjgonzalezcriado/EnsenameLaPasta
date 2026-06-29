# Analisis de Dependencias

> Skill: analisis-arquitectura | Version: 3.1.0

Patrones para analizar y gestionar dependencias en proyectos .NET.

---

## Analisis de Paquetes NuGet

```bash
# Listar paquetes por proyecto
dotnet list package

# Paquetes desactualizados
dotnet list package --outdated

# Vulnerabilidades
dotnet list package --vulnerable --include-transitive

# Dependencias transitivas
dotnet list package --include-transitive
```

## Detectar Dependencias Circulares

En Clean Architecture, las dependencias deben fluir hacia adentro:
Web -> Application -> Domain
Infrastructure -> Application -> Domain

**Regla:** Domain NO debe referenciar ningun otro proyecto.

## Metricas de Acoplamiento

| Metrica | Bueno | Malo |
|---------|-------|------|
| Referencias entre proyectos | < 5 por proyecto | > 10 |
| Paquetes NuGet | < 15 por proyecto | > 30 |
| Dependencias transitivas | Controladas via CPM | Sin gestion |
| Dependencias circulares | 0 | > 0 |

## Recomendaciones

1. **Abstracciones en Domain**: Interfaces, no implementaciones
2. **DI en Composition Root**: Solo Web/Host conoce las implementaciones
3. **CPM**: Central Package Management para versiones
4. **Audit trimestral**: dotnet list package --outdated --vulnerable

---

*Pattern v3.1.0*
