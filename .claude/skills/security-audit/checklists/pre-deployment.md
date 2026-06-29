# Checklist Pre-Despliegue - Seguridad

> Skill: security-audit | Version: 3.1.0

---

## Secretos y Configuracion

- [ ] No hay secretos hardcodeados en codigo o appsettings
- [ ] Connection strings en Azure Key Vault o variables de entorno
- [ ] appsettings.Development.json NO se despliega
- [ ] Key Vault configurado con Managed Identity

## HTTPS y TLS

- [ ] HTTPS obligatorio (UseHttpsRedirection)
- [ ] HSTS habilitado en produccion
- [ ] Certificado SSL valido y no expirado

## Autenticacion y Autorizacion

- [ ] Azure AD configurado correctamente
- [ ] Todos los endpoints protegidos con [Authorize]
- [ ] Politicas de autorizacion definidas
- [ ] CORS configurado (solo origenes permitidos)

## Validacion de Entrada

- [ ] FluentValidation en todos los endpoints POST/PUT
- [ ] Tamano maximo de request configurado
- [ ] Anti-forgery tokens en formularios

## Dependencias

- [ ] dotnet list package --vulnerable sin vulnerabilidades criticas
- [ ] Paquetes NuGet actualizados

## Logging

- [ ] Logging estructurado configurado
- [ ] No se registran datos sensibles (passwords, tokens)
- [ ] Application Insights configurado
