---
globs:
  - "**/Comillas.[!.]*[!Tests][!Tests][!.]*/**/*.cs"
  - "**/HCM.Services/**/*.cs"
  - "**/Banner/**/*.cs"
  - "**/Sigma/**/*.cs"
  - "**/EWP/**/*.cs"
  - "**/Ewp/**/*.cs"
description: Activacion condicional para consultar docs externas via Context7 cuando se edita codigo de dominio especifico
---

# Reglas para Integracion con Dominios Externos (Context7)

> **Esta regla es una plantilla**. Cada consumidor STIC.IA debe customizar la lista de dominios en `_duran/ESTADO_PROYECTO.json.dominiosExternos[]` para mapear sus integraciones reales.
> Sin customizacion, esta regla NO dispara (lista vacia → no Context7 fetches).

**Origen**: item J1 bloque J, ADR-034 (Pasada 3 Consumidores dual del workflow `claude-code-setup`). Caso uso real: ErpSync con Oracle HCM Cloud, Intercambio con EWP.

---

## Cuando aplica

Cuando Claude edita archivos en patrones especificos del consumidor (HCM.Services, EWP/, Banner/, Sigma/, etc.), debe **consultar Context7** para obtener docs actualizadas de la API externa correspondiente.

## Configuracion por proyecto

En `_duran/ESTADO_PROYECTO.json` anadir campo opcional `dominiosExternos`:

```json
{
  "dominiosExternos": [
    {
      "nombre": "Oracle HCM Cloud",
      "carpetaPattern": "HCM.Services",
      "context7Library": "oracle/oracle-hcm-cloud-rest-api",
      "razon": "API quarterly updates - schema cambia 4x al ano",
      "tools": ["resolve-library-id", "get-library-docs"]
    },
    {
      "nombre": "Banner Academic",
      "carpetaPattern": "Banner",
      "context7Library": "ellucian/banner-api",
      "razon": "Banner v9 vs v10 schema diferente"
    },
    {
      "nombre": "EWP",
      "carpetaPattern": "Ewp",
      "context7Library": "erasmus-without-paper/ewp-api",
      "razon": "Standard paneuropeo con HEI-IDs y manifest discovery"
    },
    {
      "nombre": "Sigma Nominas",
      "carpetaPattern": "Sigma",
      "context7Library": "sigma-nominas/api",
      "razon": "API legacy SOAP/REST"
    }
  ]
}
```

Si el campo NO existe o el array esta vacio: esta regla NO genera fetches Context7 automaticos.

## Comportamiento

Cuando Claude detecta edicion en `**/Comillas.{Proyecto}.{CarpetaPattern}/**/*.cs`:

1. Leer `_duran/ESTADO_PROYECTO.json.dominiosExternos[]`
2. Para cada entrada con `carpetaPattern` que matchea el path actual:
   - Antes de generar codigo, invocar `mcp__context7__resolve-library-id` con `context7Library`
   - Luego `mcp__context7__get-library-docs` con el ID resuelto
   - Usar la doc obtenida para validar firma de endpoints, schemas, tipos
3. Si Context7 no esta disponible o el library-id no resuelve: continuar con caution (no fallar)

## Cuando NO disparar

- Si el archivo es un test (`**/*Tests.cs`, `**/Tests/**`)
- Si la edicion es trivial (typo fix, comment, rename variable local)
- Si el usuario explicitamente dice "no consultes docs" o similar
- Si ya se consulto Context7 para esa libreria en los ultimos 5 mensajes (cache local de sesion)

## Configuracion avanzada (opcional)

```json
{
  "dominiosExternos": [
    {
      "nombre": "Oracle HCM Cloud",
      "carpetaPattern": "HCM.Services",
      "context7Library": "oracle/oracle-hcm-cloud-rest-api",
      "anchorTopics": ["authentication", "rate-limiting", "incremental-fetch"],
      "skipPaths": ["**/HCM.Services.Tests/**", "**/HCM.Services.Mocks/**"]
    }
  ]
}
```

- `anchorTopics`: temas especificos a consultar (pasados a `get-library-docs` como `topic` param)
- `skipPaths`: globs adicionales a excluir (ej. mocks, tests, fakes)

## Razon de existir esta regla

Las APIs externas Comillas tienen ciclos de cambio rapidos (Oracle HCM quarterly updates, EWP version anuales). Sin consultar docs frescas, Claude puede:
- Usar endpoints deprecated
- Asumir schemas que ya no existen
- Configurar auth obsoleto

Context7 fetch dirigido en el momento adecuado elimina este riesgo sin penalizar performance (solo se invoca cuando se toca codigo del dominio especifico).

## Anti-patrones

- **NO disparar para CADA archivo .cs del proyecto** — solo para los del dominio especifico (matching estricto)
- **NO consultar Context7 si la edicion es estructural** (refactor de nombres, etc.) — solo cuando se toca logica de integracion
- **NO inventar `context7Library`** — usar IDs reales que Context7 resuelve (consultar `resolve-library-id` primero)
- **NO hardcodear esta lista en el hook** — vive en `_duran/ESTADO_PROYECTO.json` para que cada proyecto la customice

## Sinergia con otros componentes

- Skill `api-integration-patterns` cubre el patron generico de integracion (HttpClient, OAuth2, retry). Esta regla complementa con docs domain-especificas.
- Skill `security-audit` puede usar docs Context7 para verificar que se aplican los patrones de auth especificos de cada API externa.

---

*Regla condicional plantilla STIC.IA - item J1 bloque J (ADR-034). Caso validado: ErpSync Oracle HCM + Intercambio EWP.*
