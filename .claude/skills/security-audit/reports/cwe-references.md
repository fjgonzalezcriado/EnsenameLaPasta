# Referencias CWE Comunes

> Skill: security-audit | Version: 3.5.0

21 CWEs mas frecuentes en auditorias de seguridad, organizados por categoria.

> Ver tambien: `owasp/owasp-top10-2025.md`, `reports/report-format.md`

---

## Tabla de CWEs

| CWE | Nombre | Categoria |
|-----|--------|-----------|
| CWE-22 | Recorrido de Rutas | Inyeccion |
| CWE-77 | Inyeccion de Comandos | Inyeccion |
| CWE-79 | Cross-Site Scripting (XSS) | Inyeccion |
| CWE-89 | Inyeccion SQL | Inyeccion |
| CWE-94 | Inyeccion de Codigo | Inyeccion |
| CWE-200 | Exposicion de Informacion | Fuga de Informacion |
| CWE-250 | Ejecucion con Privilegios Innecesarios | Control de Acceso |
| CWE-284 | Control de Acceso Inadecuado | Control de Acceso |
| CWE-310 | Problemas Criptograficos | Criptografia |
| CWE-327 | Uso de Algoritmo Criptografico Roto | Criptografia |
| CWE-352 | Falsificacion de Solicitudes entre Sitios (CSRF) | Sesion |
| CWE-359 | Violacion de Privacidad | Privacidad |
| CWE-502 | Deserializacion de Datos No Confiables | Inyeccion |
| CWE-564 | Inyeccion Hibernate | Inyeccion |
| CWE-611 | XXE (Entidad Externa XML) | Inyeccion |
| CWE-639 | Evasion de Autorizacion via Clave Controlada por Usuario | Control de Acceso |
| CWE-798 | Credenciales Hardcodeadas | Autenticacion |
| CWE-917 | Inyeccion de Lenguaje de Expresion | Inyeccion |
| CWE-918 | Falsificacion de Solicitudes del Lado del Servidor (SSRF) | Red |
| CWE-1021 | Renderizacion Inadecuada de UI (Clickjacking) | UI |
| CWE-1336 | Inyeccion de Plantillas del Lado del Servidor (SSTI) | Inyeccion |

---

## Uso en Informes

Al documentar un hallazgo, incluir siempre:

1. **Numero CWE** - Identificador unico (ej: CWE-89)
2. **Enlace** - `https://cwe.mitre.org/data/definitions/{ID}.html`
3. **Categoria OWASP** - Mapeo al Top 10 correspondiente

### Mapeo CWE -> OWASP Top 10 2025

| OWASP | CWEs Principales |
|-------|------------------|
| A01: Control de Acceso Roto | CWE-284, CWE-639, CWE-250 |
| A02: Configuracion Incorrecta | CWE-1021 |
| A03: Cadena de Suministro | CWE-502 |
| A04: Fallos Criptograficos | CWE-310, CWE-327 |
| A05: Inyeccion | CWE-79, CWE-89, CWE-77, CWE-94, CWE-611, CWE-917 |
| A07: Autenticacion | CWE-798 |
| A09: Logging | CWE-200 |
| A10: SSRF | CWE-918 |

---

*Pattern v3.7.0*
