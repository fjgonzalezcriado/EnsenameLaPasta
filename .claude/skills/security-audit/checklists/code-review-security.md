# Checklist Code Review - Seguridad

> Skill: security-audit | Version: 3.1.0

---

## Injection

- [ ] No hay SQL concatenado (usar parametros o EF Core)
- [ ] No hay interpolacion en ExecuteSqlRaw
- [ ] Inputs sanitizados antes de usar en paths de archivo

## Autenticacion

- [ ] Endpoints sensibles protegidos con [Authorize]
- [ ] No se exponen tokens en logs o respuestas
- [ ] Tokens validados correctamente (issuer, audience, expiry)

## Criptografia

- [ ] No se usan algoritmos obsoletos (MD5, SHA1 para hashing)
- [ ] Data Protection API para cifrado de datos
- [ ] Passwords hasheados con bcrypt/Argon2

## Manejo de Errores

- [ ] No se exponen stack traces en produccion
- [ ] ProblemDetails para errores de API (RFC 7807)
- [ ] Excepciones loggeadas pero no retornadas al cliente

## Archivos

- [ ] No se aceptan paths de archivo del usuario sin validar
- [ ] Uploads con validacion de tipo y tamano
- [ ] Archivos servidos desde directorio controlado
