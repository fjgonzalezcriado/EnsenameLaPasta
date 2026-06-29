# Checklist WSTG (Guia de Pruebas de Seguridad Web)

> Skill: security-audit | Version: 3.5.0

Checklist basado en OWASP Web Security Testing Guide (WSTG) v4.2, organizado en 9 categorias.

> Ver tambien: `owasp/owasp-top10-2025.md`, `checklists/api-security.md`

---

## 1. Recopilacion de Informacion

- [ ] WSTG-INFO-01: Descubrimiento y reconocimiento en motores de busqueda
- [ ] WSTG-INFO-02: Fingerprinting del servidor web
- [ ] WSTG-INFO-03: Revision de metaarchivos del servidor web (robots.txt, sitemap.xml)
- [ ] WSTG-INFO-04: Enumeracion de aplicaciones en el servidor web
- [ ] WSTG-INFO-05: Revision de contenido web para fuga de informacion
- [ ] WSTG-INFO-06: Identificacion de puntos de entrada de la aplicacion
- [ ] WSTG-INFO-07: Mapeo de rutas de ejecucion
- [ ] WSTG-INFO-08: Fingerprinting del framework de la aplicacion web
- [ ] WSTG-INFO-10: Mapeo de la arquitectura de la aplicacion

## 2. Pruebas de Configuracion

- [ ] WSTG-CONF-01: Configuracion de la infraestructura de red
- [ ] WSTG-CONF-02: Configuracion de la plataforma de la aplicacion
- [ ] WSTG-CONF-05: Enumeracion de interfaces de administracion
- [ ] WSTG-CONF-06: Pruebas de metodos HTTP
- [ ] WSTG-CONF-07: Pruebas de HSTS
- [ ] WSTG-CONF-10: Pruebas de toma de control de subdominios
- [ ] WSTG-CONF-11: Pruebas de almacenamiento en la nube
- [ ] WSTG-CONF-12: Pruebas de CSP

## 3. Pruebas de Autenticacion

- [ ] WSTG-ATHN-01: Credenciales sobre canal cifrado
- [ ] WSTG-ATHN-02: Credenciales por defecto
- [ ] WSTG-ATHN-03: Mecanismo de bloqueo debil
- [ ] WSTG-ATHN-04: Evasion del esquema de autenticacion
- [ ] WSTG-ATHN-07: Politica de contrasenas debil
- [ ] WSTG-ATHN-09: Cambio/restablecimiento de contrasena debil
- [ ] WSTG-ATHN-10: Autenticacion mas debil en canal alternativo

## 4. Pruebas de Autorizacion

- [ ] WSTG-ATHZ-01: Recorrido de directorios/inclusion de archivos
- [ ] WSTG-ATHZ-02: Evasion del esquema de autorizacion
- [ ] WSTG-ATHZ-03: Escalada de privilegios
- [ ] WSTG-ATHZ-04: Referencias directas a objetos inseguras (IDOR)

## 5. Gestion de Sesiones

- [ ] WSTG-SESS-01: Esquema de gestion de sesiones
- [ ] WSTG-SESS-02: Atributos de cookies (Secure, HttpOnly, SameSite)
- [ ] WSTG-SESS-03: Fijacion de sesion
- [ ] WSTG-SESS-05: CSRF
- [ ] WSTG-SESS-06: Funcionalidad de cierre de sesion
- [ ] WSTG-SESS-07: Tiempo de expiracion de sesion
- [ ] WSTG-SESS-09: Secuestro de sesion
- [ ] WSTG-SESS-10: Pruebas de JWT

## 6. Pruebas de Validacion de Entrada

- [ ] WSTG-INPV-01: XSS reflejado
- [ ] WSTG-INPV-02: XSS almacenado
- [ ] WSTG-INPV-03: Manipulacion de verbos HTTP
- [ ] WSTG-INPV-04: Contaminacion de parametros HTTP
- [ ] WSTG-INPV-05: Inyeccion SQL
- [ ] WSTG-INPV-06: Inyeccion LDAP
- [ ] WSTG-INPV-07: Inyeccion XML / XXE
- [ ] WSTG-INPV-09: Inyeccion XPath
- [ ] WSTG-INPV-11: Inyeccion de codigo
- [ ] WSTG-INPV-12: Inyeccion de comandos
- [ ] WSTG-INPV-15: Division/contrabando HTTP
- [ ] WSTG-INPV-17: Inyeccion de cabecera Host
- [ ] WSTG-INPV-18: Inyeccion de plantillas del lado del servidor (SSTI)
- [ ] WSTG-INPV-19: Falsificacion de solicitudes del lado del servidor (SSRF)

## 7. Criptografia

- [ ] WSTG-CRYP-01: Seguridad debil de la capa de transporte (TLS)
- [ ] WSTG-CRYP-02: Oraculo de relleno (padding oracle)
- [ ] WSTG-CRYP-03: Informacion sensible via canales no cifrados
- [ ] WSTG-CRYP-04: Cifrado debil (MD5, SHA1, DES, RC4)

## 8. Logica de Negocio

- [ ] Validacion de limites de negocio (importes maximos, cantidades)
- [ ] Pruebas de flujo de trabajo (saltar pasos, repetir acciones)
- [ ] Pruebas de tasa de solicitudes (rate limiting)

## 9. Lado del Cliente

- [ ] XSS basado en DOM
- [ ] Manipulacion de recursos del lado del cliente
- [ ] Almacenamiento inseguro en cliente (localStorage con datos sensibles)

---

## Uso

Copiar este checklist al inicio de cada auditoria WSTG y marcar cada punto como:
- **OK** / **FALTA** / **PARCIAL** / **N/A**

---

*Checklist v3.7.0*
