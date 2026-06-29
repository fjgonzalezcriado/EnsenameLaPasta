# Plantilla: Guía de Aplicación CLI

> Plantilla completa para documentación de aplicaciones de consola.
> Incluye: uso básico, comandos, opciones globales, códigos de salida, ejemplos.

---

## Plantilla Completa

**Ubicación:** `_duran/guias-uso/GUIA_USO_CLI.md`

```markdown
# Guía de Uso - CLI [Nombre]

> Manual de comandos y opciones
> Generado: [FECHA]

## Uso Básico

```bash
[ejecutable] [comando] [opciones]
```

---

## Comandos

### [comando]

[Descripción del comando]

**Sintaxis:**

```bash
[ejecutable] [comando] [argumentos] [opciones]
```

**Argumentos:**

| Argumento | Requerido | Descripción |
|-----------|-----------|-------------|
| [nombre] | [Sí/No] | [descripción] |

**Opciones:**

| Opción | Corta | Tipo | Default | Descripción |
|--------|-------|------|---------|-------------|
| --[nombre] | -[x] | [tipo] | [valor] | [descripción] |

**Ejemplos:**

```bash
# Ejemplo básico
[ejecutable] [comando] [args]

# Con opciones
[ejecutable] [comando] --opcion valor
```

---

## Opciones Globales

| Opción | Corta | Descripción |
|--------|-------|-------------|
| --help | -h | Muestra ayuda |
| --version | -v | Muestra versión |
| --verbose | | Modo detallado |
| --config | -c | Archivo de configuración |

---

## Códigos de Salida

| Código | Significado |
|--------|-------------|
| 0 | Éxito |
| 1 | Error general |
| 2 | Error de argumentos |

---

## Ejemplos de Uso Común

### [Escenario 1]

```bash
[comando completo]
```

### [Escenario 2]

```bash
[comando completo]
```
```

---

## Generación de Ejemplos

### Ejemplo curl

```bash
curl -X [METHOD] "[URL]" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '[BODY_JSON]'
```

### Colección Postman

```json
{
  "info": {
    "name": "[Nombre API]",
    "description": "Colección generada automáticamente",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "item": [
    {
      "name": "[Endpoint]",
      "request": {
        "method": "[METHOD]",
        "header": [
          {
            "key": "Authorization",
            "value": "Bearer {{token}}"
          },
          {
            "key": "Content-Type",
            "value": "application/json"
          }
        ],
        "url": {
          "raw": "{{baseUrl}}/api/[recurso]",
          "host": ["{{baseUrl}}"],
          "path": ["api", "[recurso]"]
        }
      }
    }
  ],
  "variable": [
    {
      "key": "baseUrl",
      "value": "https://api.comillas.edu"
    },
    {
      "key": "token",
      "value": ""
    }
  ]
}
```
