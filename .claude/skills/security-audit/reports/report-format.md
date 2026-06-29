# Formato de Informe y Sistema de Clasificacion

> Skill: security-audit | Version: 3.5.0

Estructura estandar para informes de auditoria de seguridad con clasificacion CVSS v3.1 y sistema de semaforos.

> Ver tambien: `reports/cwe-references.md`, `reports/mitigation-plan.md`

---

## Sistema de Clasificacion por Semaforos

### Severidad de Hallazgos

| Semaforo | Severidad | CVSS v3.1 | Descripcion | Tiempo de Remediacion |
|:--------:|-----------|-----------|-------------|----------------------|
| ROJO | **CRITICA** | 9.0 - 10.0 | Explotacion inmediata posible. Compromiso total, RCE, credenciales expuestas. | **24-48 horas** |
| NARANJA | **ALTA** | 7.0 - 8.9 | Vulnerabilidad explotable con impacto significativo. Escalada de privilegios, SQLi. | **1 semana** |
| AMARILLO | **MEDIA** | 4.0 - 6.9 | Impacto moderado o requiere condiciones especificas. Defense in depth. | **2-4 semanas** |
| VERDE | **BAJA** | 0.1 - 3.9 | Impacto minimo. Mejores practicas, hardening recomendado. | **Siguiente release** |
| BLANCO | **INFO** | N/A | Observacion sin vulnerabilidad directa. Mejora de codigo. | **Backlog** |

### Indicadores de Explotabilidad

| Indicador | Significado |
|:---------:|-------------|
| **EXPLOTABLE EXTERNAMENTE** | Atacante externo puede explotar sin autenticacion |
| **REQUIERE AUTENTICACION** | Necesita credenciales validas para explotar |
| **REQUIERE ACCESO INTERNO** | Solo explotable con acceso a infraestructura/codigo |
| **TEORICO / DEFENSE IN DEPTH** | No explotable en configuracion actual |

### Riesgo Global del Proyecto

| Clasificacion | Criterio |
|:-------------:|----------|
| **CRITICO** | 1+ hallazgos criticos O 3+ hallazgos altos |
| **ALTO** | 1-2 hallazgos altos O 5+ hallazgos medios |
| **MEDIO** | 3-4 hallazgos medios O 10+ hallazgos bajos |
| **BAJO** | Solo hallazgos bajos o informativos |

---

## Estructura del Informe

### Resumen Ejecutivo

```
## Resumen Ejecutivo

{DESCRIPCION_3_6_LINEAS_BASADA_EN_EVIDENCIAS}

* **Riesgo global:** {NIVEL_RIESGO}
* **N de hallazgos:**
  * Criticos: {N}
  * Altos: {N}
  * Medios: {N}
  * Bajos: {N}
* **Recomendacion inmediata:** {RECOMENDACION_PRIORITARIA}
```

### Hallazgo CRITICO

```
#### C{NN}: {TITULO_HALLAZGO}

| Atributo | Valor |
|----------|-------|
| **Severidad** | CRITICA |
| **CVSS v3.1** | {SCORE} - {VECTOR} |
| **Explotabilidad** | {INDICADOR} |
| **CWE** | CWE-{ID}: {NOMBRE} |
| **OWASP** | {CATEGORIA} |

**Evidencia:**
* Archivo: {RUTA_ARCHIVO}
* Lineas: {LINEA_INICIO}-{LINEA_FIN}

(fragmento de codigo vulnerable)

**Analisis de Riesgo:**
- {IMPACTO_1}
- {IMPACTO_2}

**Remediacion:**
(codigo corregido con ejemplo antes/despues)

**Referencias:**
- OWASP: {REF}
- CWE: https://cwe.mitre.org/data/definitions/{ID}.html

**Accion inmediata:** {ACCION_URGENTE}
```

### Hallazgo ALTO

```
#### A{NN}: {TITULO}

| Atributo | Valor |
|----------|-------|
| **Severidad** | ALTA |
| **CVSS v3.1** | {SCORE} |
| **CWE** | CWE-{ID} |

**Evidencia:** {RUTA}:{LINEAS}
**Riesgo:** {DESCRIPCION}
**Remediacion:** {RECOMENDACION con codigo}
```

### Hallazgo MEDIO

```
#### M{NN}: {TITULO}

| Atributo | Valor |
|----------|-------|
| **Severidad** | MEDIA |
| **CVSS v3.1** | {SCORE} |

**Evidencia:** {RUTA}:{LINEA}
**Riesgo:** {DESCRIPCION}
**Remediacion:** {RECOMENDACION}
```

### Hallazgo BAJO / INFO

```
#### B{NN}/I{NN}: {TITULO}

**Ubicacion:** {RUTA}
**Observacion:** {DESCRIPCION}
**Recomendacion:** {RECOMENDACION}
```

---

## Patrones de Seguridad Positivos

Documentar tambien los controles correctamente implementados:

```
| Patron | Ubicacion | Evaluacion |
|--------|-----------|:----------:|
| Content Security Policy (CSP) | Program.cs:XX | OK |
| HSTS | Program.cs:XX | OK |
| Anti-CSRF Tokens | Program.cs:XX | OK |
| Stored Procedures parametrizados | *Repository.cs | OK |
```

---

*Pattern v3.7.0*
