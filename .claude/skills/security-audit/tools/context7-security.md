# Integracion con Context7 para Seguridad

> Skill: security-audit | Version: 3.5.0

Como usar el servidor MCP Context7 para verificar hallazgos de seguridad contra fuentes autorizadas.

> Ver tambien: `tools/sast-integration.md`, `reports/cwe-references.md`

---

## Proposito

El servidor `context7` se usa para consultar documentacion actualizada y validar hallazgos de seguridad:

- Consultar CVEs conocidos y su estado de parcheo
- Validar versiones de dependencias contra bases de datos de vulnerabilidades
- Referenciar documentacion OWASP, NIST y guias de seguridad actualizadas
- Verificar si una version especifica de un paquete tiene vulnerabilidades publicadas
- Obtener recomendaciones de remediacion basadas en documentacion oficial

---

## Criterio de Hallazgo Verificado

Un hallazgo debe considerarse **verificado** cuando:

1. Se identifica el patron vulnerable en el codigo (ruta, lineas, fragmento real)
2. `context7` confirma que el patron corresponde a una vulnerabilidad conocida (CVE, CWE, referencia OWASP)
3. Se documenta el flujo de datos si aplica (entrada del usuario -> procesamiento -> punto de riesgo)

---

## Ejemplo de Hallazgo Verificado

```
Archivo: Controllers/UserController.cs
Lineas: 88-120

Analisis:
- Entrada controlable: Request["id"]
- No pasa por sanitizacion ni validacion
- Llega a QueryBuilder.Append() (concatenacion SQL directa)

context7: Patron corresponde a CWE-89 (SQL Injection).
Documentacion OWASP confirma riesgo en concatenacion sin parametrizacion.

Flujo confirmado: entrada de usuario -> sin sanitizacion -> consulta SQL.
Vulnerabilidad real: SQL Injection.
```

---

## Flujo de Trabajo

```
1. Identificar patron sospechoso en codigo
   |
2. Buscar en context7: documentacion de la libreria/framework
   |
3. Confirmar si el patron es vulnerable segun documentacion oficial
   |
4. Si es vulnerable: documentar con CWE, CVSS, referencia OWASP
   |
5. Si NO es vulnerable: documentar razon de descarte
```

---

## Reglas Operativas

- **No se permiten suposiciones:** cada hallazgo debe tener una ruta, fragmento o referencia real
- Si algo **no existe**, escribir exactamente: `No encontrado: <ruta>`
- Si hay secretos, marcar la ubicacion y reemplazar el contenido por `SECRETO_OMITIDO`
- No incluir instrucciones explotables. Usar descripciones de pruebas seguras
- No generar payloads de explotacion reales
- No asumir explotacion sin evidencia

---

## Uso de Context7 en Auditorias

```
# Paso 1: Resolver ID de libreria
resolve-library-id("Entity Framework Core")

# Paso 2: Consultar documentacion de seguridad
get-library-docs(id, topic="security")

# Paso 3: Verificar si un patron de codigo es seguro segun docs oficiales
```

---

*Pattern v3.7.0*
