# Checklist Seguridad API

> Skill: security-audit | Version: 3.1.0

---

## Autenticacion

- [ ] JWT Bearer token obligatorio
- [ ] Token validation (issuer, audience, lifetime)
- [ ] Refresh token flow implementado

## Rate Limiting

- [ ] Rate limiting por usuario/IP configurado
- [ ] Respuesta 429 con Retry-After header
- [ ] Limites documentados en la API

## Validacion

- [ ] Todos los inputs validados (FluentValidation)
- [ ] Tamano maximo de payload configurado
- [ ] Content-Type validado

## Output

- [ ] No se exponen IDs internos innecesariamente
- [ ] Paginacion con limite maximo
- [ ] Campos sensibles excluidos de DTOs

## CORS

- [ ] Solo origenes especificos permitidos
- [ ] No usar AllowAnyOrigin en produccion
- [ ] Credentials policy correcta

## Documentacion

- [ ] OpenAPI/Swagger solo en desarrollo
- [ ] Endpoints de health/info no exponen datos sensibles
