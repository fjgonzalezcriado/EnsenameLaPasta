# Checklist de Verificacion Post-Remediacion

> Skill: security-audit | Version: 3.5.0

11 verificaciones para confirmar que las correcciones de seguridad se aplicaron correctamente.

> Ver tambien: `reports/mitigation-plan.md`, `reports/report-format.md`

---

## Verificacion por Severidad

```
Hallazgos Criticos:
- [ ] C01: {DESCRIPCION} - Verificado por: ______ Fecha: ______
- [ ] C02: {DESCRIPCION} - Verificado por: ______ Fecha: ______

Hallazgos Altos:
- [ ] A01: {DESCRIPCION} - Verificado por: ______ Fecha: ______
- [ ] A02: {DESCRIPCION} - Verificado por: ______ Fecha: ______

Hallazgos Medios:
- [ ] M01: {DESCRIPCION} - Verificado por: ______ Fecha: ______
```

---

## Validaciones Generales

1. - [ ] Credenciales rotadas y verificadas
2. - [ ] Secretos movidos a gestor seguro (Key Vault / User Secrets)
3. - [ ] Dependencias vulnerables actualizadas
4. - [ ] Tests de seguridad ejecutados y pasando
5. - [ ] Escaneo SAST limpio (sin nuevos hallazgos criticos/altos)
6. - [ ] Penetration test realizado (si aplica)
7. - [ ] Configuracion de cabeceras de seguridad verificada
8. - [ ] Pipeline CI/CD actualizado con controles de seguridad
9. - [ ] Documentacion de seguridad actualizada
10. - [ ] Equipo notificado de los cambios
11. - [ ] Plan de monitorizacion post-despliegue definido

---

## Plantilla de Verificacion por Hallazgo

```
### Verificacion: {ID_HALLAZGO}

| Campo | Valor |
|-------|-------|
| Hallazgo original | {DESCRIPCION} |
| Severidad | {CRITICA/ALTA/MEDIA/BAJA} |
| Archivo corregido | {RUTA}:{LINEAS} |
| Correccion aplicada | {DESCRIPCION_CORRECCION} |
| Verificado por | {NOMBRE} |
| Fecha verificacion | {YYYY-MM-DD} |
| Test de regresion | {PASA/FALLA} |
| Nueva vulnerabilidad introducida | {SI/NO} |

Evidencia: {ENLACE_A_COMMIT_O_PR}
```

---

## Criterios de Cierre

Un hallazgo se considera **cerrado** cuando:

1. La correccion esta desplegada en el entorno afectado
2. El escaneo SAST ya no detecta el hallazgo
3. Una segunda persona ha verificado la correccion
4. No se han introducido nuevas vulnerabilidades
5. Los tests de regresion pasan correctamente

---

*Checklist v3.7.0*
