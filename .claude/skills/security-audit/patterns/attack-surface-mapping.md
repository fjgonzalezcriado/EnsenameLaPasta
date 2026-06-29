# Mapeo de Superficie de Ataque

> Skill: security-audit | Version: 3.5.0

Documentar todos los puntos de entrada, flujos de datos y componentes expuestos de la aplicacion.

> Ver tambien: `tools/sast-integration.md`, `reports/report-format.md`

---

## Endpoints Detectados

Documentar todos los endpoints con su estado de seguridad:

```
| Ruta | Metodo | Autenticacion | Autorizacion | Validacion | Riesgo |
|------|--------|:-------------:|:------------:|:----------:|:------:|
| /api/{endpoint} | POST | OK | OK | OK | BAJO |
| /admin/{endpoint} | GET | OK | PARCIAL | OK | MEDIO |
| /public/{endpoint} | GET | NO | NO | PARCIAL | ALTO |
```

---

## Flujos de Datos Sensibles

Documentar el recorrido de datos sensibles a traves de la arquitectura:

```
[Usuario] --HTTPS--> [WAF/LB] --HTTP--> [App Server] --TDS/Encrypt--> [SQL Server]
                                              |
                                              +--> [IdP] (OAuth 2.0 / SAML)
                                              +--> [APIs Externas] (Basic Auth / OAuth / API Key)
```

---

## Analisis de CI/CD y Pipeline

```
| Archivo | Riesgos | Estado |
|---------|---------|:------:|
| .github/workflows/ci.yml | Secretos en logs, permisos excesivos | ALTO |
| azure-pipelines.yml | Sin escaneo SAST | MEDIO |
| Dockerfile | Imagen base desactualizada | MEDIO |
```

### Recomendaciones de Seguridad en Pipeline

- Implementar escaneo SAST en PR
- Implementar escaneo SCA de dependencias
- No almacenar secretos en codigo
- Usar variables de entorno cifradas
- Implementar firma de artefactos
- Usar imagenes base minimales y actualizadas

---

## Plantilla de Mapeo

### Paso 1: Inventario de Tecnologias

```
| Componente | Tecnologia | Version | Exposicion |
|------------|-----------|---------|:----------:|
| Frontend | React / Blazor | X.Y | Publica |
| API | .NET 10 | 10.0 | Publica |
| Base de datos | SQL Server | 2022 | Interna |
| Cache | Redis | 7.x | Interna |
| IdP | Azure AD | - | Externa |
```

### Paso 2: Puntos de Entrada

```
| Punto de Entrada | Tipo | Datos Recibidos | Autenticacion |
|------------------|------|-----------------|:-------------:|
| /api/auth/login | POST | email, password | No |
| /api/becas | GET/POST | query params, JSON | Si |
| /api/files/upload | POST | multipart/form-data | Si |
| WebSocket /hub | WS | mensajes JSON | Si |
```

### Paso 3: Datos Sensibles

```
| Dato | Clasificacion | Donde se almacena | Cifrado |
|------|:-------------:|-------------------|:-------:|
| Contrasenas | CRITICO | SQL Server (hash) | bcrypt |
| DNI/NIE | ALTO | SQL Server | Si (AES) |
| Email | MEDIO | SQL Server | No |
| Tokens JWT | ALTO | Memoria + cookie | HMAC |
```

---

*Pattern v3.7.0*
