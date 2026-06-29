# Guia de Diagramas de Arquitectura

> Skill: analisis-arquitectura | Version: 3.1.0

Guia para crear diagramas de arquitectura usando el modelo C4 y Mermaid.

---

## Modelo C4

| Nivel | Que muestra | Audiencia |
|-------|-------------|-----------|
| Context | Sistema y sus interacciones externas | Todos |
| Container | Aplicaciones y almacenes de datos | Tecnica |
| Component | Componentes internos de un container | Desarrolladores |
| Code | Clases y relaciones | Desarrolladores |

## Nivel 1: Context

```mermaid
graph TB
    User[fa:fa-user Usuario] --> WebApp[Aplicacion Web]
    Admin[fa:fa-user-cog Admin] --> WebApp
    WebApp --> API[API Backend]
    API --> DB[(SQL Server)]
    API --> AzureAD[Azure AD]
    API --> Banner[Banner API]
```

## Nivel 2: Container

```mermaid
graph TB
    subgraph Azure
        AppService[App Service - API]
        SQL[(Azure SQL)]
        KeyVault[Key Vault]
        Blob[Blob Storage]
    end
    subgraph On-Premise
        IIS[IIS - Web App]
    end
    IIS --> AppService
    AppService --> SQL
    AppService --> KeyVault
    AppService --> Blob
```

## Convenciones de Color

| Elemento | Color |
|----------|-------|
| Usuario | Azul claro |
| Sistema propio | Azul |
| Sistema externo | Gris |
| Base de datos | Verde |
| Almacenamiento | Amarillo |

---

*Pattern v3.1.0*
