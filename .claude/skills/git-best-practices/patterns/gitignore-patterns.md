# Patrones .gitignore

> Skill: git-best-practices | Version: 3.5.0

Plantillas .gitignore optimizadas por tipo de proyecto.

---

## .NET (.NET 10 / .NET 4.x)

```gitignore
## Build results
[Bb]in/
[Oo]bj/
[Dd]ebug/
[Rr]elease/
x64/
x86/

## Visual Studio
.vs/
*.suo
*.user
*.userosscache
*.sln.docstates
launchSettings.json

## NuGet
**/packages/*
!**/packages/build/
*.nupkg
**/[Pp]ackages/
project.lock.json
project.fragment.lock.json

## User-specific
*.rsuser
*.suo
*.user
*.sln.docstates

## Test Results
[Tt]est[Rr]esult*/
[Dd]ebugPublic/
TestResult.xml
coverage/
*.coverage
*.coveragexml

## Publish
publish/
PublishOutput/

## Secrets (CRITICO)
appsettings.Development.json
appsettings.Local.json
secrets.json
*.pfx
*.key
*.pem

## Rider
.idea/
*.sln.iml

## Mac
.DS_Store

## Windows
Thumbs.db
ehthumbs.db
Desktop.ini
```

---

## Node.js / Frontend

```gitignore
## Dependencies
node_modules/
bower_components/

## Build
dist/
build/
.next/
.nuxt/
out/

## Environment
.env
.env.local
.env.*.local

## Logs
npm-debug.log*
yarn-debug.log*
yarn-error.log*

## IDE
.vscode/
!.vscode/extensions.json
!.vscode/settings.json
.idea/

## OS
.DS_Store
Thumbs.db

## Test coverage
coverage/
.nyc_output/
```

---

## Monorepo

```gitignore
## Root-level ignores
node_modules/
.env
.env.local

## .NET projects
**/[Bb]in/
**/[Oo]bj/
**/.vs/

## Node projects
**/node_modules/
**/dist/
**/build/

## Shared
*.log
coverage/
.DS_Store
Thumbs.db
```

---

## Reglas Importantes

### Archivos que NUNCA deben estar en Git

```gitignore
# Secretos y credenciales
*.pfx
*.key
*.pem
*.p12
appsettings.*.json    # Excepto appsettings.json base
.env
.env.*
secrets/

# Archivos grandes
*.zip
*.tar.gz
*.rar
*.7z
*.exe
*.dll         # Solo binarios, no los de build
*.msi
```

### Archivos que SI deben estar en Git

```
# Configuracion base (sin secretos)
appsettings.json
.editorconfig
.gitattributes
Directory.Build.props
Directory.Packages.props
global.json
nuget.config
```

---

## Limpiar archivos ya trackeados

```bash
# Si un archivo ya esta en Git y lo añades a .gitignore,
# debes quitarlo del tracking:
git rm --cached appsettings.Development.json
git commit -m "chore: remove tracked secrets from git"

# Para limpieza masiva:
git rm -r --cached .
git add .
git commit -m "chore: apply updated .gitignore"
```

---

*Pattern v3.7.0*
