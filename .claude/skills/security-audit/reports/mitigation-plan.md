# Plan de Mitigacion Priorizado

> Skill: security-audit | Version: 3.5.0

Plantilla para organizar las acciones de remediacion en 4 horizontes temporales segun severidad.

> Ver tambien: `reports/report-format.md`, `checklists/post-remediation.md`

---

## Horizonte Inmediato (24-48 horas) - CRITICO

Acciones que deben ejecutarse **antes de cualquier otra cosa**.

```
| # | Accion | Hallazgo | Responsable | Estado |
|:-:|--------|----------|-------------|:------:|
| 1 | Rotar credenciales expuestas | C01 | {NOMBRE} | Pendiente |
| 2 | Parchear vulnerabilidad RCE | C02 | {NOMBRE} | Pendiente |
| 3 | Desactivar endpoint vulnerable | C03 | {NOMBRE} | Pendiente |
```

---

## Horizonte Corto (1 semana) - ALTO

Correcciones que requieren cambios de codigo con despliegue urgente.

```
| # | Accion | Hallazgo | Responsable | Estado |
|:-:|--------|----------|-------------|:------:|
| 1 | Implementar parametrizacion SQL | A01 | {NOMBRE} | Pendiente |
| 2 | Corregir configuracion CORS | A02 | {NOMBRE} | Pendiente |
| 3 | Añadir validacion de autorizacion | A03 | {NOMBRE} | Pendiente |
```

---

## Horizonte Medio (2-4 semanas) - MEDIO

Mejoras que requieren planificacion y no son urgentes.

```
| # | Accion | Hallazgo | Responsable | Estado |
|:-:|--------|----------|-------------|:------:|
| 1 | Implementar cabeceras de seguridad completas | M01 | {NOMBRE} | Pendiente |
| 2 | Mejorar logging de seguridad | M02 | {NOMBRE} | Pendiente |
| 3 | Configurar rate limiting | M03 | {NOMBRE} | Pendiente |
```

---

## Horizonte Largo (Siguiente release) - BAJO

Mejoras de hardening y buenas practicas.

```
| # | Accion | Hallazgo | Responsable | Estado |
|:-:|--------|----------|-------------|:------:|
| 1 | Refactorizar validacion de entrada | B01 | {NOMBRE} | Pendiente |
| 2 | Actualizar dependencias menores | B02 | {NOMBRE} | Pendiente |
| 3 | Implementar SAST en pipeline CI/CD | B03 | {NOMBRE} | Pendiente |
```

---

## Seguimiento

### Reunion de Seguimiento

- **Criticos**: seguimiento diario hasta cierre
- **Altos**: seguimiento cada 2-3 dias
- **Medios**: seguimiento semanal
- **Bajos**: revision en siguiente sprint planning

### Metricas

```
| Metrica | Valor |
|---------|-------|
| Total hallazgos | {N} |
| Cerrados | {N} ({%}) |
| En progreso | {N} |
| Pendientes | {N} |
| Tiempo medio de remediacion (criticos) | {N} horas |
| Tiempo medio de remediacion (altos) | {N} dias |
```

---

*Pattern v3.7.0*
