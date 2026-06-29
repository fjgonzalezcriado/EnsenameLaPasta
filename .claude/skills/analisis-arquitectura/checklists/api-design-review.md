# Checklist - Review de Diseno API

> Skill: analisis-arquitectura | Version: 3.1.0

---

## Convenciones REST

- [ ] URLs en plural y kebab-case (/api/becas, /api/tipos-beca)
- [ ] Verbos HTTP correctos (GET=leer, POST=crear, PUT=actualizar, DELETE=eliminar)
- [ ] Codigos de estado apropiados (200, 201, 204, 400, 401, 403, 404, 500)
- [ ] ProblemDetails (RFC 7807) para errores

## Versionado

- [ ] Estrategia de versionado definida (URL, header, o query)
- [ ] Versiones anteriores documentadas con fecha de deprecacion

## Paginacion y Filtrado

- [ ] Paginacion en todos los endpoints de listado
- [ ] Limite maximo de pageSize (ej: 100)
- [ ] Filtrado y ordenacion parametrizados
- [ ] Respuesta incluye metadatos de paginacion

## Autenticacion / Autorizacion

- [ ] Todos los endpoints protegidos (excepto health)
- [ ] Politicas de autorizacion granulares
- [ ] Rate limiting implementado
- [ ] CORS configurado correctamente

## Documentacion

- [ ] OpenAPI/Swagger actualizado
- [ ] Ejemplos de request/response
- [ ] Errores documentados
- [ ] Postman collection disponible

## Idempotencia

- [ ] POST con idempotency key (si aplica)
- [ ] PUT es idempotente
- [ ] DELETE es idempotente (204 si no existe)

---

*Checklist v3.1.0*
