# Patrones de GitHub Actions

> Skill: git-best-practices | Version: 3.5.0

Reusable Workflows, Composite Actions, matrices y secretos para proyectos Comillas.

---

## Arquitectura de Workflows

```
.github/
├── workflows/
│   ├── ci.yml                    # CI principal (build + test)
│   ├── deploy.yml                # Deploy a entornos
│   └── reusable-dotnet-build.yml # Workflow reutilizable
├── actions/
│   └── setup-dotnet/             # Composite Action
│       └── action.yml
└── CODEOWNERS
```

### Principio de Organizacion

| Tipo | Uso | Ejemplo |
|------|-----|---------|
| **Reusable Workflow** | Pipeline completa reutilizable | Build + Test + Publish |
| **Composite Action** | Pasos repetidos dentro de un job | Setup .NET + cache |
| **Regular Workflow** | Pipeline especifica del repo | CI, Deploy |

---

## Workflow CI para .NET 10

Ver `templates/github-workflow.yml.template` para la plantilla completa.

### Resumen del flujo

```yaml
name: CI
on:
  push:
    branches: [main]
  pull_request:
    branches: [main]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: dotnet restore
      - run: dotnet build --no-restore
      - run: dotnet test --no-build --verbosity normal
```

---

## Reusable Workflows

### Definir (workflow_call)

```yaml
# .github/workflows/reusable-dotnet-build.yml
name: .NET Build and Test (Reusable)
on:
  workflow_call:
    inputs:
      dotnet-version:
        required: false
        type: string
        default: '10.0.x'
      solution-path:
        required: true
        type: string
    secrets:
      NUGET_API_KEY:
        required: false

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ inputs.dotnet-version }}

      - name: Cache NuGet
        uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: nuget-${{ hashFiles('**/*.csproj') }}
          restore-keys: nuget-

      - run: dotnet restore ${{ inputs.solution-path }}
      - run: dotnet build --no-restore ${{ inputs.solution-path }}
      - run: dotnet test --no-build ${{ inputs.solution-path }}
```

### Consumir

```yaml
# .github/workflows/ci.yml
name: CI
on:
  push:
    branches: [main]
  pull_request:

jobs:
  build:
    uses: ./.github/workflows/reusable-dotnet-build.yml
    with:
      solution-path: src/Comillas.App.sln
    secrets: inherit
```

---

## Composite Actions

### Definir

```yaml
# .github/actions/setup-dotnet/action.yml
name: Setup .NET with Cache
description: Setup .NET SDK with NuGet cache

inputs:
  dotnet-version:
    description: '.NET SDK version'
    required: false
    default: '10.0.x'

runs:
  using: composite
  steps:
    - uses: actions/setup-dotnet@v4
      with:
        dotnet-version: ${{ inputs.dotnet-version }}

    - name: Cache NuGet
      uses: actions/cache@v4
      with:
        path: ~/.nuget/packages
        key: nuget-${{ runner.os }}-${{ hashFiles('**/*.csproj') }}
        restore-keys: nuget-${{ runner.os }}-

    - name: Restore
      shell: bash
      run: dotnet restore
```

### Consumir

```yaml
steps:
  - uses: actions/checkout@v4
  - uses: ./.github/actions/setup-dotnet
    with:
      dotnet-version: '10.0.x'
  - run: dotnet build
```

---

## Matrices

```yaml
jobs:
  test:
    strategy:
      matrix:
        os: [ubuntu-latest, windows-latest]
        dotnet: ['8.0.x', '10.0.x']
        exclude:
          - os: windows-latest
            dotnet: '8.0.x'
    runs-on: ${{ matrix.os }}
    steps:
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ matrix.dotnet }}
      - run: dotnet test
```

---

## Secretos

```yaml
# Buenas practicas con secretos
env:
  # NUNCA hardcodear
  CONNECTION_STRING: ${{ secrets.DB_CONNECTION_STRING }}
  AZURE_CLIENT_ID: ${{ secrets.AZURE_CLIENT_ID }}

# Usar environments para separar secretos por entorno
jobs:
  deploy-staging:
    environment: staging
    # Los secretos de "staging" estan disponibles

  deploy-production:
    environment: production
    # Los secretos de "production" estan disponibles
    # Puede requerir aprobacion manual
```

---

## Buenas Practicas

| Practica | Descripcion |
|----------|-------------|
| Pin de versiones | `uses: actions/checkout@v4` (no `@main`) |
| Cache agresivo | NuGet, node_modules, build outputs |
| Fail fast | `strategy.fail-fast: true` en matrices |
| Concurrency | Cancelar workflows duplicados |
| Path filters | Solo ejecutar cuando cambian archivos relevantes |
| Timeout | Siempre definir `timeout-minutes` |

```yaml
# Concurrency: cancelar runs anteriores del mismo PR
concurrency:
  group: ${{ github.workflow }}-${{ github.ref }}
  cancel-in-progress: true
```

---

*Pattern v3.7.0*
