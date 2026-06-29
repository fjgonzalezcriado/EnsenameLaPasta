# Plantilla: Guía de Consumidor API

> Plantilla completa para documentación de APIs REST orientada a consumidores.
> Incluye: autenticación, endpoints, códigos de estado, flujos comunes, paginación.

---

## Plantilla Completa

**Ubicación:** `_duran/guias-uso/GUIA_USO_API.md`

```markdown
# Guía de Uso - API [Nombre]

> Documentación funcional para consumidores de la API
> Generado: [FECHA]

## Autenticación

**Tipo:** [Bearer Token / API Key / OAuth2 / Azure AD]
**Header:** `Authorization: Bearer {token}`
**Obtener token:** [Endpoint o instrucciones]

---

## Endpoints

### [MÓDULO]

#### [VERBO] /api/[recurso]

[Descripción breve de qué hace este endpoint]

**Parámetros:**

| Parámetro | Ubicación | Tipo | Requerido | Descripción | Ejemplo |
|-----------|-----------|------|-----------|-------------|---------|
| [nombre] | [query/path/body] | [tipo] | [Sí/No] | [descripción] | [ejemplo] |

**Ejemplo curl:**

```bash
curl -X [VERBO] "https://api.comillas.edu/api/[recurso]" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{"campo": "valor"}'
```

**Respuesta exitosa ([CÓDIGO]):**

```json
{
  "success": true,
  "data": { ... }
}
```

**Errores posibles:**

| Código | Tipo | Descripción | Solución |
|--------|------|-------------|----------|
| 400 | Bad Request | Datos inválidos | Verificar formato |
| 401 | Unauthorized | Token inválido | Renovar token |
| 403 | Forbidden | Sin permisos | Verificar rol |
| 404 | Not Found | Recurso no existe | Verificar ID |
| 422 | Unprocessable | Validación negocio | Revisar reglas |

---

## Códigos de Estado Globales

| Código | Significado | Cuándo ocurre |
|--------|-------------|---------------|
| 200 | OK | Operación exitosa (GET, PUT) |
| 201 | Created | Recurso creado (POST) |
| 204 | No Content | Eliminación exitosa (DELETE) |
| 400 | Bad Request | Datos de entrada inválidos |
| 401 | Unauthorized | Token faltante o inválido |
| 403 | Forbidden | Sin permisos para la acción |
| 404 | Not Found | Recurso no existe |
| 409 | Conflict | Conflicto (ej: duplicado) |
| 422 | Unprocessable | Validación de negocio fallida |
| 500 | Server Error | Error interno (reportar a soporte) |

---

## Flujos Comunes

### [Nombre del Flujo]

```
1. [VERBO] /api/[endpoint1] → [Descripción]
2. [VERBO] /api/[endpoint2] → [Descripción]
3. [VERBO] /api/[endpoint3] → [Descripción]
```

---

## Paginación

Todos los endpoints de listado soportan paginación:

| Parámetro | Tipo | Default | Descripción |
|-----------|------|---------|-------------|
| page | int | 1 | Número de página |
| pageSize | int | 20 | Elementos por página |

**Respuesta:**

```json
{
  "items": [...],
  "totalItems": 100,
  "page": 1,
  "pageSize": 20,
  "totalPages": 5
}
```

---

## Contacto

- **Soporte:** soporte.stic@comillas.edu
- **Documentación técnica:** [Link a SPEC o README]
```
