# Firmado de Commits con SSH

> Skill: git-best-practices | Version: 3.5.0

Configuracion de firmado SSH para commits y tags (Git 2.34+, recomendado sobre GPG).

---

## Por que Firmar Commits

- **Verificacion de identidad**: Prueba que el commit fue hecho por quien dice ser
- **Integridad**: Garantiza que el contenido no fue alterado
- **Compliance**: Requisito en entornos regulados y cadena de suministro de software
- **GitHub/GitLab**: Muestra insignia "Verified" en commits

---

## Requisitos

- Git 2.34 o superior (preferible 2.52+)
- OpenSSH 8.8 o superior
- Clave SSH ED25519 (recomendada) o RSA 4096

---

## Configuracion Paso a Paso

### 1. Generar clave SSH (si no existe)

```bash
# ED25519 (recomendado - mas seguro y rapido)
ssh-keygen -t ed25519 -C "usuario@comillas.edu" -f ~/.ssh/id_ed25519_signing

# RSA 4096 (alternativa compatible)
ssh-keygen -t rsa -b 4096 -C "usuario@comillas.edu" -f ~/.ssh/id_rsa_signing
```

### 2. Configurar Git para usar SSH signing

```bash
# Formato de firma: SSH
git config --global gpg.format ssh

# Clave publica para firmar
git config --global user.signingkey ~/.ssh/id_ed25519_signing.pub

# Auto-firmar commits
git config --global commit.gpgsign true

# Auto-firmar tags
git config --global tag.gpgsign true
```

### 3. Configurar allowed_signers (verificacion local)

```bash
# Crear archivo de firmantes autorizados
# Formato: email algorithm key
echo "usuario@comillas.edu $(cat ~/.ssh/id_ed25519_signing.pub)" >> ~/.ssh/allowed_signers

# Decirle a Git donde esta
git config --global gpg.ssh.allowedSignersFile ~/.ssh/allowed_signers
```

### 4. Registrar clave en GitHub/GitLab

**GitHub:**
1. Settings > SSH and GPG keys > New SSH key
2. Key type: **Signing Key**
3. Pegar contenido de `~/.ssh/id_ed25519_signing.pub`

**GitLab:**
1. Preferences > SSH Keys
2. Usage type: **Authentication & Signing**
3. Pegar clave publica

**Azure DevOps:**
- Azure DevOps no soporta verificacion de firmas SSH nativamente
- Usar GPG como alternativa si se requiere verificacion en ADO

---

## Verificacion

```bash
# Verificar firma de un commit
git log --show-signature -1

# Verificar firma de un tag
git tag -v v1.2.0

# Ver commits con estado de firma
git log --format='%H %G? %aN %s'
# G = Good signature
# B = Bad signature
# U = Untrusted
# N = No signature
```

---

## Configuracion por Proyecto

```bash
# Si se necesita una clave diferente para un proyecto especifico
cd /ruta/proyecto
git config --local user.signingkey ~/.ssh/id_ed25519_comillas.pub
git config --local commit.gpgsign true
```

---

## Alternativa: GPG Signing

```bash
# Solo usar si Azure DevOps requiere verificacion o si SSH no es viable

# Generar clave GPG
gpg --full-generate-key
# Elegir RSA 4096, email de Comillas

# Configurar Git
gpg --list-secret-keys --keyid-format=long
git config --global user.signingkey <KEY_ID>
git config --global commit.gpgsign true
```

---

## Troubleshooting

| Problema | Solucion |
|----------|----------|
| `error: Load key: invalid format` | Verificar que se usa la clave publica (.pub) |
| `error: ssh-keygen not found` | Instalar OpenSSH 8.8+ |
| Commits no muestran "Verified" en GitHub | Verificar que la clave esta registrada como "Signing Key" |
| `error: gpg.ssh.allowedSignersFile` | Crear el archivo con `echo "email $(cat key.pub)" >> allowed_signers` |

---

*Pattern v3.7.0*
