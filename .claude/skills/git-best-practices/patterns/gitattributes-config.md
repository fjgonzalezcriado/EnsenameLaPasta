# Configuracion .gitattributes

> Skill: git-best-practices | Version: 3.5.0

Line endings, LFS, diff drivers y merge strategies.

---

## .gitattributes para .NET

```gitattributes
# Auto-detect text files and normalize line endings
* text=auto

# Source code - LF
*.cs text diff=csharp
*.csx text diff=csharp
*.vb text diff=csharp
*.razor text
*.cshtml text
*.xaml text
*.json text
*.xml text
*.config text
*.props text
*.targets text
*.md text
*.yml text
*.yaml text
*.sql text
*.html text
*.css text
*.js text
*.ts text

# Solution files - CRLF (Visual Studio requirement)
*.sln text eol=crlf
*.slnx text eol=crlf
*.csproj text eol=crlf
*.vbproj text eol=crlf
*.fsproj text eol=crlf

# Shell scripts - LF
*.sh text eol=lf
*.bash text eol=lf

# PowerShell - CRLF
*.ps1 text eol=crlf
*.psm1 text eol=crlf
*.psd1 text eol=crlf

# Binary files (no diff, no merge)
*.png binary
*.jpg binary
*.jpeg binary
*.gif binary
*.ico binary
*.svg binary
*.woff binary
*.woff2 binary
*.ttf binary
*.eot binary
*.pdf binary
*.zip binary
*.dll binary
*.exe binary
*.pdb binary
*.nupkg binary

# Git LFS (large files)
# Descomentar si se usa Git LFS:
# *.psd filter=lfs diff=lfs merge=lfs -text
# *.ai filter=lfs diff=lfs merge=lfs -text
# *.mp4 filter=lfs diff=lfs merge=lfs -text
# *.mov filter=lfs diff=lfs merge=lfs -text

# Linguist overrides (para estadisticas de GitHub)
Documentos_Base/** linguist-documentation
*.template linguist-generated
```

---

## Git LFS

### Cuando usar LFS

| Archivo | LFS | Razon |
|---------|-----|-------|
| Imagenes > 1MB | Si | Binario grande |
| Videos | Si | Binarios grandes |
| PDFs generados | Si | Cambian frecuentemente |
| Archivos PSD/AI | Si | Binarios grandes |
| DLLs de terceros | Si | Binarios |
| NuGet packages | No | Usar NuGet restore |
| node_modules | No | Usar npm install |

### Configuracion LFS

```bash
# Instalar Git LFS
git lfs install

# Trackear tipos de archivos
git lfs track "*.psd"
git lfs track "*.mp4"
git lfs track "*.pdf"

# El .gitattributes se actualiza automaticamente
# Commit del .gitattributes
git add .gitattributes
git commit -m "chore: configure Git LFS tracking"
```

---

## Merge Strategies por Archivo

```gitattributes
# Archivos que no deben mergearse automaticamente
project.lock.json merge=ours
*.designer.cs merge=ours

# Archivos generados - siempre regenerar
*.g.cs -merge
*.generated.cs -merge
```

---

*Pattern v3.7.0*
