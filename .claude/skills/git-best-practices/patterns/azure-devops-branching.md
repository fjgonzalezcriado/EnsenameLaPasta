# Configuracion de Branching en Azure DevOps

> Skill: git-best-practices | Version: 3.7.0
> Documento fuente: `Guias/reglas-externas/branching-strategies-v2-guia-completa.html`

Configuracion practica de estrategias de ramificacion en Azure DevOps para proyectos Comillas.

---

## Ranking de Compatibilidad con Azure DevOps

| Estrategia | Compatibilidad | Notas |
|------------|---------------|-------|
| **GitHub Flow** | Excelente | Branch policies + build validation nativos |
| **GitHub Flow simplificado** | Excelente | Build validation only, sin reviewers, merge local --no-ff |
| **Release Flow** | Excelente | Cherry-pick UI integrado, release branches |
| **OneFlow** | Muy bueno | Tags + branch policies, sin tooling extra |
| **Trunk-Based** | Bueno | Requiere feature flags externos (Azure App Configuration) |
| **Developer Flow** | Bueno | Multi-branch pipelines, merge entre ramas |
| **GitLab Flow** | Bueno | Ramas por entorno, Azure Pipelines multi-stage equivalente |
| **GitFlow** | Con friccion | Requiere disciplina manual, no hay soporte nativo develop/release |

---

## Branch Policies (Proteccion de Ramas)

### GitHub Flow - Branch Policies para `main`

```json
{
  "branch": "main",
  "policies": {
    "minimumApprovers": {
      "enabled": true,
      "count": 1,
      "allowDownvotes": false,
      "creatorVoteCounts": false,
      "resetOnSourcePush": true
    },
    "buildValidation": {
      "enabled": true,
      "builds": [
        {
          "pipeline": "CI-Build",
          "trigger": "automatic",
          "filenamePatterns": ["*.cs", "*.csproj", "*.sln"]
        }
      ]
    },
    "commentResolution": {
      "enabled": true,
      "requiredForAll": true
    },
    "mergeStrategy": {
      "allowSquash": true,
      "allowRebase": false,
      "allowMerge": false,
      "allowRebaseMerge": false
    },
    "workItemLinking": {
      "enabled": false
    }
  }
}
```

### Release Flow - Branch Policies para `main` + `release/*`

```json
{
  "branches": {
    "main": {
      "minimumApprovers": 1,
      "buildValidation": true,
      "mergeStrategy": "noFastForward",
      "commentResolution": true
    },
    "release/*": {
      "minimumApprovers": 2,
      "buildValidation": true,
      "mergeStrategy": "noFastForward",
      "commentResolution": true,
      "requireLinkedWorkItems": true
    }
  }
}
```

**Regla clave Release Flow**: Hotfixes siempre van a `main` primero, luego cherry-pick a `release/*`.

---

## Pipelines YAML por Estrategia

### GitHub Flow - Pipeline CI/CD

```yaml
# azure-pipelines.yml - GitHub Flow
trigger:
  branches:
    include:
      - main
  paths:
    exclude:
      - '**/*.md'
      - 'docs/**'

pr:
  branches:
    include:
      - main

pool:
  vmImage: 'windows-latest'

variables:
  buildConfiguration: 'Release'
  dotnetVersion: '10.x'

stages:
  # ═══════════════════════════════════════════
  # STAGE 1: Build + Test (siempre)
  # ═══════════════════════════════════════════
  - stage: Build
    displayName: 'Build & Test'
    jobs:
      - job: BuildAndTest
        steps:
          - task: UseDotNet@2
            inputs:
              version: $(dotnetVersion)

          - script: dotnet restore
            displayName: 'Restore'

          - script: dotnet build --configuration $(buildConfiguration) --no-restore
            displayName: 'Build'

          - script: dotnet test --configuration $(buildConfiguration) --no-build --logger trx --collect:"XPlat Code Coverage"
            displayName: 'Test'

          - task: PublishTestResults@2
            inputs:
              testResultsFormat: 'VSTest'
              testResultsFiles: '**/*.trx'

          - task: PublishCodeCoverageResults@2
            inputs:
              summaryFileLocation: '**/coverage.cobertura.xml'

          - script: dotnet publish --configuration $(buildConfiguration) --output $(Build.ArtifactStagingDirectory)
            displayName: 'Publish'
            condition: and(succeeded(), eq(variables['Build.SourceBranch'], 'refs/heads/main'))

          - publish: $(Build.ArtifactStagingDirectory)
            artifact: drop
            condition: and(succeeded(), eq(variables['Build.SourceBranch'], 'refs/heads/main'))

  # ═══════════════════════════════════════════
  # STAGE 2: Deploy Demo (auto desde main)
  # ═══════════════════════════════════════════
  - stage: DeployDemo
    displayName: 'Deploy → Demo'
    dependsOn: Build
    condition: and(succeeded(), eq(variables['Build.SourceBranch'], 'refs/heads/main'))
    jobs:
      - deployment: DeployToDemo
        environment: 'Demo'
        strategy:
          runOnce:
            deploy:
              steps:
                - task: IISWebAppDeploymentOnMachineGroup@0
                  inputs:
                    WebSiteName: 'MiApp-Demo'
                    Package: '$(Pipeline.Workspace)/drop/**/*.zip'

  # ═══════════════════════════════════════════
  # STAGE 3: Deploy Produccion (manual)
  # ═══════════════════════════════════════════
  - stage: DeployProd
    displayName: 'Deploy → Produccion'
    dependsOn: DeployDemo
    condition: and(succeeded(), eq(variables['Build.SourceBranch'], 'refs/heads/main'))
    jobs:
      - deployment: DeployToProd
        environment: 'Produccion'  # Requiere aprobacion manual
        strategy:
          runOnce:
            deploy:
              steps:
                - task: IISWebAppDeploymentOnMachineGroup@0
                  inputs:
                    WebSiteName: 'MiApp'
                    Package: '$(Pipeline.Workspace)/drop/**/*.zip'
```

### Release Flow - Pipeline con Release Branches

```yaml
# azure-pipelines.yml - Release Flow
trigger:
  branches:
    include:
      - main
      - release/*

pr:
  branches:
    include:
      - main
      - release/*

stages:
  - stage: Build
    displayName: 'Build & Test'
    jobs:
      - job: BuildAndTest
        steps:
          - task: UseDotNet@2
            inputs:
              version: '10.x'
          - script: dotnet restore && dotnet build -c Release && dotnet test -c Release --no-build
            displayName: 'Restore → Build → Test'

          - script: dotnet publish -c Release -o $(Build.ArtifactStagingDirectory)
            displayName: 'Publish'
          - publish: $(Build.ArtifactStagingDirectory)
            artifact: drop

  # Deploy desde release/* a Demo
  - stage: DeployDemo
    dependsOn: Build
    condition: and(succeeded(), startsWith(variables['Build.SourceBranch'], 'refs/heads/release/'))
    jobs:
      - deployment: DeployToDemo
        environment: 'Demo'
        strategy:
          runOnce:
            deploy:
              steps:
                - task: IISWebAppDeploymentOnMachineGroup@0
                  inputs:
                    WebSiteName: 'MiApp-Demo'
                    Package: '$(Pipeline.Workspace)/drop/**/*.zip'

  # Deploy desde release/* a Produccion (con aprobacion)
  - stage: DeployProd
    dependsOn: DeployDemo
    condition: and(succeeded(), startsWith(variables['Build.SourceBranch'], 'refs/heads/release/'))
    jobs:
      - deployment: DeployToProd
        environment: 'Produccion'
        strategy:
          runOnce:
            deploy:
              steps:
                - task: IISWebAppDeploymentOnMachineGroup@0
                  inputs:
                    WebSiteName: 'MiApp'
                    Package: '$(Pipeline.Workspace)/drop/**/*.zip'
```

### GitHub Flow Simplificado - Pipeline Solo Developer

```yaml
# azure-pipelines.yml - GitHub Flow simplificado (solo developer)
# Merge local con --no-ff, sin PRs formales
# Build validation como unica branch policy
trigger:
  branches:
    include:
      - main
      - release/*

pool:
  vmImage: 'windows-latest'

stages:
  - stage: BuildAndDeploy
    jobs:
      - job: Build
        steps:
          - task: UseDotNet@2
            inputs:
              version: '10.x'

          - script: dotnet restore && dotnet build -c Release && dotnet test -c Release --no-build
            displayName: 'Build & Test'

          - script: dotnet publish -c Release -o $(Build.ArtifactStagingDirectory)
            displayName: 'Publish'

          - publish: $(Build.ArtifactStagingDirectory)
            artifact: drop

      # Deploy automatico a Demo
      - deployment: DeployDemo
        dependsOn: Build
        environment: 'Demo'
        strategy:
          runOnce:
            deploy:
              steps:
                - task: IISWebAppDeploymentOnMachineGroup@0
                  inputs:
                    WebSiteName: 'MiApp-Demo'
                    Package: '$(Pipeline.Workspace)/drop/**/*.zip'

      # Deploy manual a Produccion
      - deployment: DeployProd
        dependsOn: DeployDemo
        environment: 'Produccion'
        strategy:
          runOnce:
            deploy:
              steps:
                - task: IISWebAppDeploymentOnMachineGroup@0
                  inputs:
                    WebSiteName: 'MiApp'
                    Package: '$(Pipeline.Workspace)/drop/**/*.zip'
```

---

## Ejemplo: Infraestructura Comillas

### Topologia tipica

```
                    ┌─────────────────────────────────┐
                    │        Azure DevOps             │
                    │   ┌─────────────────────────┐   │
                    │   │    Build Pipeline        │   │
                    │   │  (build + test + publish) │   │
                    │   └────────────┬────────────┘   │
                    └────────────────┼────────────────┘
                                     │
                    ┌────────────────┼────────────────┐
                    │                │                 │
                    ▼                ▼                 ▼
            ┌──────────────┐ ┌──────────────┐ ┌──────────────────┐
            │     DEV      │ │    DEMO      │ │      PRO         │
            │ (automatico) │ │ (automatico) │ │ (aprobacion)     │
            │              │ │              │ │                  │
            │ trapster01   │ │ demowww      │ │ stryfe01         │
            │ stiltman01   │ │ (comillas.   │ │ stryfe02         │
            │ (devwww/     │ │  edu)        │ │ (balanceado)     │
            │  devws)      │ │              │ │                  │
            └──────────────┘ └──────────────┘ └──────────────────┘
```

### Flujo por Estrategia

| Estrategia | Dev | Demo | Pro |
|------------|-----|------|-----|
| **GitHub Flow** | PR → main → auto | main → auto | main → manual (aprobacion) |
| **Release Flow** | PR → main → auto | release/* → auto | release/* → manual |
| **Solo developer** | main → auto | main → auto | main → manual |

---

## Merge Queues

### Concepto

Las merge queues garantizan que los merges a `main` no introducen regresiones, incluso con PRs concurrentes.

```
Sin merge queue:           Con merge queue:
PR-A ──merge── main ✅     PR-A ──queue── build(A) ──merge── main ✅
PR-B ──merge── main ❌     PR-B ──queue── build(A+B) ──merge── main ✅
(conflicto no detectado)   (conflicto detectado ANTES del merge)
```

### En Azure DevOps

Azure DevOps no tiene merge queue nativa (a diferencia de GitHub). Alternativas:

1. **Build validation obligatoria** - Cada PR debe pasar build contra main actualizado
2. **Auto-complete con policies** - PR se completa automaticamente cuando pasa todas las policies
3. **Herramientas externas** - Mergify, Aviator (compatible con Azure Repos)

### En GitHub (Merge Queue GA)

```json
{
  "merge_queue": {
    "enabled": true,
    "merge_method": "squash",
    "max_entries_to_build": 5,
    "min_entries_to_merge": 1,
    "build_concurrency_group": "default"
  }
}
```

---

## Feature Flags con Azure App Configuration

Para estrategias que requieren feature flags (especialmente Trunk-Based):

```csharp
// Program.cs - .NET 10
builder.Configuration.AddAzureAppConfiguration(options =>
{
    options.Connect(builder.Configuration["AzureAppConfig:ConnectionString"])
           .UseFeatureFlags(flagOptions =>
           {
               flagOptions.CacheExpirationInterval = TimeSpan.FromMinutes(5);
           });
});

builder.Services.AddFeatureManagement();
```

```csharp
// Uso en controlador
[FeatureGate("NuevoFiltrosBecas")]
[HttpGet("filtros-avanzados")]
public async Task<IActionResult> FiltrosAvanzados()
{
    // Solo accesible si el feature flag esta activo
}
```

```csharp
// Uso en servicio
public class BecaService
{
    private readonly IFeatureManager _featureManager;

    public async Task<List<BecaDto>> GetBecasAsync()
    {
        if (await _featureManager.IsEnabledAsync("NuevoAlgoritmoOrdenacion"))
        {
            return await GetBecasConNuevoAlgoritmoAsync();
        }
        return await GetBecasClasicasAsync();
    }
}
```

---

## Configuracion por Tipo de Proyecto

### API REST / Portal web

```
Estrategia: GitHub Flow
Branch policies: 1 reviewer, build validation, squash merge
Pipeline: main → Demo (auto) → Pro (manual)
Feature flags: Opcional
```

### App con releases planificadas

```
Estrategia: Release Flow
Branch policies: 1 reviewer (main), 2 reviewers (release/*)
Pipeline: main → Dev (auto), release/* → Demo (auto) → Pro (manual)
Feature flags: No necesarios
Cherry-pick: main primero, luego release/*
```

### Desarrollador unico

```
Estrategia: GitHub Flow simplificado
Branch policies: Build validation only (sin reviewers)
Pipeline: main → Demo (auto) → Pro (manual)
Feature flags: No
PRs: Opcionales (feature branches para cambios grandes)
```

---

## Conexion con /commit, /git-sync y /liberar

Claude lee la seccion `branching` de `_duran/ESTADO_PROYECTO.json` para:

| Comando | Usa campo | Comportamiento |
|---------|-----------|---------------|
| `/commit` | `mergeStrategy`, `convencionRamas` | Genera mensaje y sugiere merge strategy |
| `/git-sync` | `ramaBase`, `estrategia` | Sugiere flujo de sincronizacion correcto |
| `/liberar` | `releasesBranches`, `flujoDespliegue` | Guia el proceso de release segun estrategia |

---

*Pattern v3.7.0 — Azure DevOps branching configuration*
