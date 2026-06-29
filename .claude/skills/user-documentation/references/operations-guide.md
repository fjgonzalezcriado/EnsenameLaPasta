# Plantilla: Guía de Operaciones (Servicio/Worker)

> Plantilla completa para documentación de servicios Windows y Worker Services.
> Incluye: instalación, configuración, logs, monitorización, troubleshooting.

---

## Plantilla Completa

**Ubicación:** `_duran/guias-uso/GUIA_USO_SERVICIO.md`

```markdown
# Guía de Uso - Servicio [Nombre]

> Manual de operación y configuración
> Generado: [FECHA]

## Instalación

### Windows Service

```powershell
sc create "[NombreServicio]" binPath="C:\Services\[Nombre]\[Ejecutable].exe"
sc config "[NombreServicio]" start=auto
sc start "[NombreServicio]"
```

### Verificar Estado

```powershell
sc query "[NombreServicio]"
```

---

## Configuración (appsettings.json)

| Parámetro | Tipo | Default | Descripción |
|-----------|------|---------|-------------|
| [nombre] | [tipo] | [valor] | [descripción] |

**Ejemplo:**

```json
{
  "[Sección]": {
    "[Parámetro]": "[Valor]"
  }
}
```

---

## Logs

**Ubicación:** [Ruta a logs]
**Rotación:** [Diaria/Semanal]
**Retención:** [Días]

### Niveles de Log

| Nivel | Cuándo se usa |
|-------|---------------|
| Information | Operaciones normales |
| Warning | Situaciones anómalas |
| Error | Errores que requieren atención |

---

## Monitorización

- **Health Check:** [URL si aplica]
- **Métricas:** [Application Insights / otro]

---

## Troubleshooting

| Síntoma | Causa probable | Solución |
|---------|---------------|----------|
| [síntoma] | [causa] | [solución] |
```
