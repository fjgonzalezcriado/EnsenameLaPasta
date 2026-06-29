# Lista de Verificacion de Practicas de Codificacion Segura

> Skill: security-audit | Version: 3.5.0

40 verificaciones organizadas en 9 categorias. Basado en OWASP Secure Coding Practices.

> Ver tambien: `owasp/owasp-top10-2025.md`, `checklists/code-review-security.md`

---

## 1. Validacion de Entrada

- [ ] Validar en el lado del servidor (nunca confiar en el cliente)
- [ ] Usar enfoque de lista permitida (whitelist)
- [ ] Validar tipo de dato, longitud, rango, conjunto de caracteres
- [ ] Rechazar o sanitizar caracteres peligrosos
- [ ] Verificar recorrido de rutas (`../` y variantes codificadas)

## 2. Codificacion de Salida

- [ ] Codificar la salida segun el contexto (HTML, JavaScript, URL, CSS, SQL)
- [ ] Usar funciones de codificacion integradas del framework
- [ ] Sanitizar HTML con bibliotecas probadas (DOMPurify, OWASP Java HTML Sanitizer)

## 3. Autenticacion

- [ ] Autenticacion multifactor implementada
- [ ] Sin credenciales por defecto en produccion
- [ ] Contrasenas hasheadas con bcrypt/scrypt/Argon2
- [ ] Bloqueo de cuenta tras intentos fallidos
- [ ] Flujo seguro de restablecimiento de contrasena
- [ ] IDs de sesion rotados tras el inicio de sesion

## 4. Gestion de Sesiones

- [ ] IDs de sesion solo en cookies (nunca en URLs)
- [ ] Cookies configuradas con flags Secure, HttpOnly, SameSite
- [ ] Sesiones invalidadas al cerrar sesion
- [ ] Tiempos de inactividad y absolutos configurados
- [ ] IDs de sesion regenerados tras cambios de privilegio

## 5. Criptografia

- [ ] Algoritmos fuertes (AES-256-GCM, RSA-2048+, SHA-256+)
- [ ] Sin claves/secretos hardcodeados
- [ ] Gestion y rotacion adecuada de claves
- [ ] TLS 1.2+ obligatorio
- [ ] Validacion de certificados no eludida

## 6. Manejo de Errores

- [ ] Mensajes de error genericos para los usuarios
- [ ] Errores detallados registrados solo en el servidor
- [ ] Sin trazas de pila en respuestas de produccion
- [ ] Fallo seguro (denegar acceso en caso de error)
- [ ] Liberar recursos asignados en bloques finally/defer

## 7. Proteccion de Datos

- [ ] Datos sensibles cifrados en reposo y en transito
- [ ] Sin datos sensibles en URLs ni logs
- [ ] Clasificacion adecuada de datos implementada
- [ ] Secretos almacenados en almacen seguro/vault (no en codigo)
- [ ] Datos personales tratados segun regulaciones de privacidad (RGPD/LOPDGDD)

## 8. Operaciones con Archivos

- [ ] Subidas de archivos validadas (tipo, tamano, nombre)
- [ ] Archivos subidos almacenados fuera de la raiz web
- [ ] Nombres de archivo sanitizados (eliminar componentes de ruta)
- [ ] Escaneo antimalware en subidas
- [ ] Content-Disposition: attachment para descargas

## 9. Gestion de Memoria

- [ ] Tamano de buffer validado antes de operaciones
- [ ] Sin uso de funciones vulnerables conocidas (gets, strcpy)
- [ ] Recursos liberados correctamente (sin fugas de memoria)
- [ ] Verificaciones de desbordamiento de enteros para calculos

---

## Uso en Auditorias

Al ejecutar una auditoria de seguridad, copiar esta checklist y marcar cada punto como:
- **OK** - Implementado correctamente
- **FALTA** - No implementado (indicar severidad)
- **PARCIAL** - Implementado parcialmente (indicar que falta)
- **N/A** - No aplica al proyecto

---

*Pattern v3.7.0*
