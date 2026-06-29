# Integracion de Herramientas SAST

> Skill: security-audit | Version: 3.5.0

Configuracion e integracion de herramientas de analisis estatico de seguridad (SAST) en el ciclo de desarrollo.

> Ver tambien: `patterns/secure-coding-checklist.md`, `reports/report-format.md`

---

## .NET - Analizadores Integrados

```xml
<!-- En .csproj - Habilitar analizadores de seguridad -->
<PropertyGroup>
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <AnalysisLevel>latest-all</AnalysisLevel>
</PropertyGroup>

<!-- SecurityCodeScan - Analizador SAST para .NET -->
<ItemGroup>
    <PackageReference Include="SecurityCodeScan.VS2019" Version="5.*">
        <PrivateAssets>all</PrivateAssets>
        <IncludeAssets>runtime; build; native; contentfiles; analyzers</IncludeAssets>
    </PackageReference>
</ItemGroup>
```

```bash
# Escaneo de vulnerabilidades en dependencias .NET
dotnet list package --vulnerable --include-transitive
```

---

## Semgrep (Multi-lenguaje)

```bash
# Escaneo de seguridad general
semgrep --config p/default

# Escaneos especificos por lenguaje
semgrep scan --config p/python
semgrep scan --config p/javascript
semgrep scan --config p/golang
semgrep scan --config p/ruby
semgrep scan --config p/java
semgrep scan --config p/csharp

# Reglas OWASP
semgrep --config p/owasp-top-ten

# Multiples configuraciones
semgrep scan --config reglas.yaml --config mas_reglas.yaml
```

### Regla Semgrep Personalizada (Inyeccion SQL)

```yaml
rules:
  - id: deteccion-inyeccion-sql
    languages: [java]
    severity: ERROR
    mode: taint
    message: Potencial inyeccion SQL - entrada del usuario fluye hacia consulta SQL
    options:
      taint_assume_safe_booleans: true
      taint_assume_safe_numbers: true
      interfile: true
    pattern-sources:
      - patterns:
          - pattern: |
              $X(..., $SRC, ...) { ... }
          - focus-metavariable: $SRC
    pattern-sinks:
      - pattern: Statement.executeQuery(...)
      - pattern: Statement.execute(...)
    pattern-sanitizers:
      - pattern: PreparedStatement.setString(...)
```

---

## Otras Herramientas SAST

```bash
# Python - Bandit
bandit -r /ruta/al/codigo

# JavaScript - ESLint con plugin de seguridad
eslint --config .eslintrc.security.js src/

# Go - GoSec
gosec ./...

# Dependencias multi-lenguaje
npm audit                    # Node.js
pip-audit                    # Python
safety check                 # Python
snyk test                    # Multi-lenguaje
trivy fs .                   # Escaneo de sistema de archivos

# Infraestructura como Codigo
checkov --directory .        # Terraform, CloudFormation, K8s
tfsec .                      # Especifico de Terraform
```

---

## Integracion en CI/CD

### GitHub Actions

```yaml
# .github/workflows/security.yml
name: Security Scan
on: [pull_request]

jobs:
  sast:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Semgrep SAST
        uses: returntocorp/semgrep-action@v1
        with:
          config: p/owasp-top-ten

      - name: .NET Vulnerability Scan
        run: dotnet list package --vulnerable --include-transitive

      - name: Trivy File System Scan
        uses: aquasecurity/trivy-action@master
        with:
          scan-type: fs
          scan-ref: .
```

### Azure DevOps

```yaml
# azure-pipelines.yml
steps:
  - task: DotNetCoreCLI@2
    displayName: 'Vulnerability Scan'
    inputs:
      command: custom
      custom: list
      arguments: 'package --vulnerable --include-transitive'

  - script: semgrep --config p/owasp-top-ten --json -o semgrep-results.json
    displayName: 'Semgrep SAST Scan'
```

---

## Checklist de Integracion SAST

- [ ] Analizadores .NET habilitados en .csproj (AnalysisLevel: latest-all)
- [ ] SecurityCodeScan instalado como NuGet en proyectos .NET
- [ ] Semgrep configurado con reglas OWASP en pipeline CI
- [ ] `dotnet list package --vulnerable` ejecutado en cada build
- [ ] Escaneo de dependencias (Trivy/Snyk) en pipeline
- [ ] Resultados de SAST bloquean merge si hay hallazgos criticos/altos

---

*Pattern v3.7.0*
