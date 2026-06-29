# Estrategia de Versionado de Paquetes

> Skill: nugets-management | Version: 3.1.0

---

## SemVer 2.0

| Cambio | Bump | Ejemplo |
|--------|------|---------|
| Breaking change | Major | 2.0.0 -> 3.0.0 |
| Nueva funcionalidad | Minor | 2.1.0 -> 2.2.0 |
| Bug fix | Patch | 2.1.1 -> 2.1.2 |

## Floating Versions (CPM)

| Notation | Significado |
|----------|-------------|
| 9.0.* | Ultimo patch de 9.0 |
| 9.* | Ultimo minor de 9 |
| [9.0.0, 10.0.0) | Rango: >= 9.0.0 y < 10.0.0 |

## Lock Files

```xml
<!-- En .csproj o Directory.Build.props -->
<PropertyGroup>
  <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
</PropertyGroup>
```

Genera `packages.lock.json` que asegura builds reproducibles.
En CI: `dotnet restore --locked-mode`

## Cadencia Recomendada

- **Seguridad**: Inmediato (critico/alto)
- **Frameworks (.NET)**: Al publicar LTS
- **Librerias**: Mensual, con tests
- **Audit completo**: Trimestral

---

*Pattern v3.1.0*
