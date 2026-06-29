Genera un reporte completo de métricas de calidad del proyecto

Genera un reporte completo de metricas de calidad del proyecto.

---

## Uso

```bash
# Reporte basico
claude> /metrics

# Reporte con analisis de tendencias
claude> /metrics --trend

# Exportar a JSON
claude> /metrics --export json

# Exportar a HTML
claude> /metrics --export html

# Solo NuGets
claude> /metrics --nugets

# Solo complejidad
claude> /metrics --complexity
```

---

## Descripcion

El comando `/metrics` analiza el proyecto y genera un reporte completo de metricas de calidad:

### Metricas Generales
- Numero de proyectos en solucion
- Archivos C# totales
- Lineas de codigo (LOC)
- Tests unitarios (cantidad y cobertura)

### Complejidad
- Complejidad ciclomatica promedio
- Metodos >20 lineas
- Clases >500 lineas
- Profundidad de herencia

### Dependencias NuGet
- Paquetes instalados
- Vulnerabilidades conocidas (CVEs)
- Paquetes desactualizados
- Licencias de paquetes

### Deuda Tecnica
- TODOs en codigo
- FIXMEs pendientes
- Warnings de compilacion
- Code smells detectados

### Tendencias (si --trend)
- Comparacion con metricas previas
- Graficos de evolucion
- Mejoras/empeoramientos

---

## Implementacion

Claude ejecuta los siguientes comandos automaticamente:

```powershell
# 1. Contar proyectos y archivos
Get-ChildItem -Path "03_Desarrollo" -Recurse -Filter "*.csproj" | Measure-Object
Get-ChildItem -Path "03_Desarrollo" -Recurse -Filter "*.cs" | Measure-Object

# 2. Lineas de codigo
Get-ChildItem -Path "03_Desarrollo" -Recurse -Filter "*.cs" |
    Get-Content | Measure-Object -Line

# 3. Tests y cobertura
dotnet test --collect:"Code Coverage" --no-build

# 4. Analisis de complejidad (via Roslyn)
dotnet build /p:RunAnalyzers=true /p:TreatWarningsAsErrors=false

# 5. Vulnerabilidades NuGet
dotnet list package --vulnerable --include-transitive

# 6. Paquetes desactualizados
dotnet list package --outdated

# 7. Deuda tecnica
Get-ChildItem -Path "03_Desarrollo" -Recurse -Filter "*.cs" |
    Select-String -Pattern "TODO|FIXME|HACK" |
    Group-Object Pattern |
    Select-Object Count, Name

# 8. Warnings de compilacion
dotnet build 2>&1 | Select-String "warning"
```

---

## Ejemplo de Output

```
╔═══════════════════════════════════════════════════════════════════╗
║  📊 METRICAS DE CALIDAD - Comillas.MiProyecto                      ║
╚═══════════════════════════════════════════════════════════════════╝

METRICAS GENERALES:
  Proyectos:           15
  Archivos C#:         487
  Lineas de codigo:    45,230
  Tests unitarios:     128
  Cobertura tests:     67% ⬆️ (+4% vs mes anterior)

COMPLEJIDAD:
  Complejidad ciclomatica promedio: 3.2 (🟢 BUENO)
  Metodos >20 lineas:   45 (9% del total)
  Clases >500 lineas:   3 (⚠️ revisar)
  Profundidad herencia: max 4 niveles

DEPENDENCIAS NUGET:
  Paquetes:            42
  Vulnerabilidades:    🔴 1 CRITICA, 2 ALTAS
    - System.Text.Json 6.0.0 → CVE-2024-12345 (upgrade a 8.0.5)
    - Newtonsoft.Json 12.0.3 → CVE-2024-67890 (upgrade a 13.0.3)
  Desactualizados:     5 paquetes
  Licencias:           41 MIT, 1 Apache-2.0

DEUDA TECNICA:
  TODOs:               23
  FIXMEs:              7
  HACKs:               2 (⚠️ revisar urgente)
  Warnings:            12

TENDENCIAS (vs 2026-01-16):
  Lineas codigo:       +2,450 (↑5.7%)
  Cobertura tests:     +4% (↑6.3%)
  Complejidad:         -0.3 (↓8.6% - MEJORA)
  Vulnerabilidades:    +1 (⚠️ EMPEORADO)

╔═══════════════════════════════════════════════════════════════════╗
║  💡 RECOMENDACIONES                                                ║
╚═══════════════════════════════════════════════════════════════════╝

1. 🔴 CRITICO: Actualizar System.Text.Json a 8.0.5 (CVE critico)
2. ⚠️  ALTA: Revisar 3 clases >500 lineas (refactorizar)
3. ⚠️  ALTA: Resolver 2 HACKs en codigo
4. 🟡 MEDIA: Aumentar cobertura tests al 75% (objetivo)
5. 🟡 MEDIA: Reducir 23 TODOs pendientes

Exportado a: _duran/metrics/2026-02-16_metrics.json
```

---

## Exportar Metricas

### JSON
```bash
claude> /metrics --export json
```

Genera: `_duran/metrics/2026-02-16_metrics.json`

```json
{
  "fecha": "2026-02-16T10:30:00Z",
  "proyecto": "Comillas.MiProyecto",
  "version": "1.2.0",
  "metricas": {
    "generales": {
      "proyectos": 15,
      "archivos_cs": 487,
      "lineas_codigo": 45230,
      "tests": 128,
      "cobertura": 67
    },
    "complejidad": {
      "ciclomatica_promedio": 3.2,
      "metodos_largos": 45,
      "clases_grandes": 3
    },
    "nugets": {
      "total": 42,
      "vulnerabilidades": {
        "criticas": 1,
        "altas": 2,
        "medias": 0,
        "bajas": 0
      },
      "desactualizados": 5
    },
    "deuda_tecnica": {
      "todos": 23,
      "fixmes": 7,
      "hacks": 2,
      "warnings": 12
    }
  }
}
```

### HTML
```bash
claude> /metrics --export html
```

Genera: `_duran/metrics/2026-02-16_reporte.html`

Reporte visual con:
- Graficos de barras (Chart.js)
- Tabla interactiva
- Colores por severidad
- Exportable a PDF

---

## Tendencias

Si existe historial de metricas previas en `_duran/metrics/`, Claude compara:

```
EVOLUCION ULTIMOS 3 MESES:

Lineas de Codigo:
  Ene: 42,500 ━━━━━━━━━━━━━━━━━━━━━━━━━━━ 100%
  Feb: 45,230 ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ 106%
  Mar: 47,800 ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ 112%

Cobertura Tests:
  Ene: 63% ━━━━━━━━━━━━━━━━━━━━━━━━━━
  Feb: 67% ━━━━━━━━━━━━━━━━━━━━━━━━━━━━
  Mar: 71% ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Vulnerabilidades:
  Ene: 0 🟢
  Feb: 3 🔴 (+3 - EMPEORADO)
  Mar: 1 🟡 (-2 - MEJORA)
```

---

## Integracion con SonarQube

Si el proyecto tiene SonarQube configurado:

```bash
claude> /metrics --sonar
```

Claude consulta la API de SonarQube y añade metricas adicionales:
- Bugs detectados
- Code smells
- Security hotspots
- Technical debt (dias)
- Duplicacion de codigo

Requiere: `sonar-scanner.properties` en raiz del proyecto

---

## Configuracion

### Umbrales personalizados

Crear archivo `.claude/metrics-config.json`:

```json
{
  "umbrales": {
    "cobertura_minima": 75,
    "complejidad_maxima": 10,
    "metodo_max_lineas": 20,
    "clase_max_lineas": 500,
    "warnings_maximos": 0
  },
  "exports": {
    "auto_export_json": true,
    "carpeta": "_duran/metrics"
  },
  "nugets": {
    "ignorar_warnings": ["NETSDK1138"],
    "solo_vulnerabilidades_altas": false
  }
}
```

---

## Notas

- **Performance**: El comando puede tardar 1-3 minutos en proyectos grandes (>100 proyectos)
- **Privacidad**: Los JSONs exportados NO contienen codigo fuente, solo metricas numericas
- **CI/CD**: Puede ejecutarse en pipeline Azure DevOps/GitHub Actions
- **Historico**: Se recomienda ejecutar `/metrics --export json` mensualmente para tracking

---

## Ejemplos de Uso

### Analisis rapido
```bash
claude> /metrics
# Output en consola, sin exportar
```

### Reporte mensual completo
```bash
claude> /metrics --trend --export html
# Genera HTML con tendencias
# Guardar en: _duran/metrics/2026-02-16_reporte.html
```

### Solo NuGets (rapido)
```bash
claude> /metrics --nugets
# Solo analiza dependencias, ignora resto
# Util para auditorias de seguridad
```

### Comparar con SonarQube
```bash
claude> /metrics --sonar --export json
# Combina metricas locales + SonarQube
```

---

## Ver Tambien

- `/nugets` - Gestion avanzada de NuGets
- `/test` - Ejecutar tests con cobertura
- `/analizar` - Analisis de codigo especifico
- Skill: `security-audit` - Auditoria de seguridad OWASP
