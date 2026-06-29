# Simulacion de Ataques y Analisis de Explotabilidad

> Skill: security-audit | Version: 3.5.0

Plantilla para documentar escenarios de ataque verificados y su explotabilidad real.

> **IMPORTANTE:** Esta seccion documenta escenarios de ataque SOLO para vulnerabilidades verificadas como explotables. NO se incluyen vulnerabilidades teoricas. No se generan payloads de explotacion reales.

> Ver tambien: `reports/report-format.md`, `reports/cwe-references.md`

---

## Clasificacion de Explotabilidad

| Categoria | Descripcion |
|-----------|-------------|
| **Explotable Externamente** | Atacante externo sin autenticacion puede explotar |
| **Requiere Autenticacion** | Necesita credenciales validas de la aplicacion |
| **Requiere Acceso Interno** | Necesita acceso al repositorio, servidor o red interna |
| **No Explotable** | Problema de codigo pero sin vector de ataque viable |

---

## Plantilla de Simulacion de Ataque

```
#### {ID}: {TITULO_VULNERABILIDAD}

| Atributo | Valor |
|----------|-------|
| **Explotabilidad** | {CATEGORIA} |
| **Severidad Real** | {NIVEL} |
| **CVSS v3.1** | {SCORE} |
| **Prerequisitos** | {LISTA_PREREQUISITOS} |

**Ubicacion del Codigo Vulnerable:**
Archivo: {RUTA_ARCHIVO}
Lineas: {LINEAS}

**Codigo Vulnerable:**
(fragmento de codigo)

**Simulacion Paso a Paso:**

| Paso | Accion | Herramienta | Resultado Esperado |
|:----:|--------|-------------|-------------------|
| 1 | {ACCION_1} | {HERRAMIENTA} | {RESULTADO} |
| 2 | {ACCION_2} | {HERRAMIENTA} | {RESULTADO} |
| 3 | {ACCION_3} | {HERRAMIENTA} | {RESULTADO} |

**Evidencia de Explotacion:**
- {EVIDENCIA_1}
- {EVIDENCIA_2}

**Impacto Demostrado:**
- {IMPACTO_CONCRETO}

**Mitigacion Inmediata:**
(codigo de mitigacion)
```

---

## Matriz de Riesgo

```
| Hallazgo | Vector de Ataque | Prerequisitos | Impacto | Probabilidad | Riesgo Final |
|----------|------------------|---------------|:-------:|:------------:|:------------:|
| C01 | Acceso a repo | Ninguno | Alto | Alta | CRITICO |
| A01 | Autenticado + Admin | Credenciales | Alto | Media | ALTO |
| M01 | Config manipulada | Acceso interno | Medio | Baja | MEDIO |
```

---

## Analisis de No-Explotabilidad

Cuando una vulnerabilidad de codigo **NO es explotable**, documentar el analisis:

```
| Factor | Analisis | Conclusion |
|--------|----------|------------|
| Origen del dato | Viene de configuracion estatica | No controlable por usuario |
| Existe endpoint que modifique? | No existe endpoint de config en runtime | Sin vector de entrada |
| Valores parametrizados? | Usa parametros Dapper/EF | Correctamente parametrizado |

Flujo de datos:
config estatica -> inyeccion en startup -> variable en codigo -> sin control de usuario

Conclusion: No se detecta flujo de datos controlable por usuario. Riesgo teorico (Defense in Depth).
```

---

## Reglas para Simulaciones

1. Solo simular vulnerabilidades con evidencia de codigo real
2. No generar payloads de explotacion funcionales
3. Describir pruebas en terminos de verificacion, no de ataque
4. Herramientas permitidas: curl, Burp Suite (modo pasivo), OWASP ZAP (scan automatico)
5. Documentar siempre el contexto de autorizacion del test

---

*Pattern v3.7.0*
