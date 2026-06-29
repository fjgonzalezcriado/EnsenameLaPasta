# Catalogo de Diagramas Mermaid

> Skill: documentacion-tecnica | Version: 3.1.0

Diagramas reutilizables para documentacion tecnica de proyectos .NET.

---

## C4 Context

```mermaid
graph TB
    User[fa:fa-user Usuario] --> System[Sistema]
    System --> ExtAPI[API Externa]
    System --> DB[(Base de Datos)]
    System --> Email[Servicio Email]
```

## Sequence - Request API

```mermaid
sequenceDiagram
    Cliente->>+API: POST /api/recurso
    API->>+Validator: Validar request
    Validator-->>-API: OK
    API->>+Handler: Ejecutar comando
    Handler->>+Repository: Guardar
    Repository->>+DB: INSERT
    DB-->>-Repository: OK
    Repository-->>-Handler: Entity
    Handler-->>-API: DTO
    API-->>-Cliente: 201 Created
```

## ER Diagram

```mermaid
erDiagram
    ENTIDAD_PRINCIPAL ||--o{ DETALLE : tiene
    ENTIDAD_PRINCIPAL {
        int Id PK
        string Nombre
        datetime CreatedAt
    }
    DETALLE {
        int Id PK
        int PrincipalId FK
        string Valor
    }
```

## State Diagram

```mermaid
stateDiagram-v2
    [*] --> Borrador
    Borrador --> Pendiente: Enviar
    Pendiente --> Aprobado: Aprobar
    Pendiente --> Rechazado: Rechazar
    Rechazado --> Borrador: Revisar
    Aprobado --> [*]
```

---

*Pattern v3.1.0*
