Genera colecciones Postman desde endpoints API

Genera colecciones Postman, environments y tests automáticos para APIs .NET.

---

## Uso

```
/postman                    → Menú interactivo
/postman collection         → Genera colección desde controllers
/postman environment        → Genera environments (Dev/Pre/Pro)
/postman tests              → Añade tests a colección existente
/postman full               → Todo: colección + environments + tests
/postman swagger [url]      → Importa desde OpenAPI/Swagger
```

---

## Instrucciones para Claude

### Al ejecutar `/postman` (sin parámetros)

Mostrar menú interactivo:

```
📦 POSTMAN COLLECTION GENERATOR
═══════════════════════════════════════════════════════════════

¿Qué deseas generar?

[1] 📋 Colección completa (collection + environments + tests)
[2] 📁 Solo colección (desde controllers)
[3] 🌍 Solo environments (Dev/Pre/Pro)
[4] ✅ Solo tests (añadir a colección existente)
[5] 📥 Importar desde Swagger/OpenAPI

Selecciona una opción (1-5):
```

### Al ejecutar `/postman full` o seleccionar opción 1

#### Paso 1: Análisis

```
Analizando proyecto...

Buscando controllers en:
  → 03_Desarrollo/**/*Controller.cs
  → **/*Controller.cs
```

Detectar:
1. **Controllers**: Buscar archivos `*Controller.cs`
2. **Rutas**: Extraer `[Route]`, `[HttpGet]`, `[HttpPost]`, etc.
3. **Parámetros**: `[FromBody]`, `[FromQuery]`, `[FromRoute]`
4. **DTOs**: Tipos de request/response
5. **Autenticación**: `[Authorize]`, JWT, Azure AD

#### Paso 2: Mostrar resumen

```
✅ Detectados X controllers:
   - NombreController (N endpoints)
   - ...

✅ Autenticación: JWT Bearer / Azure AD / Ninguna

✅ Environments en appsettings:
   - Development: https://localhost:XXXX
   - ...
```

#### Paso 3: Generar archivos

Crear en `06_Documentacion/Postman/`:

1. **`{Proyecto}_collection.json`**
   - Un folder por controller
   - Un request por endpoint
   - Ejemplos de body para POST/PUT
   - Tests automáticos según tipo

2. **`{Proyecto}_environment_dev.json`**
   - baseUrl de appsettings.Development.json
   - Variables para desarrollo

3. **`{Proyecto}_environment_pre.json`** (si existe config)

4. **`{Proyecto}_environment_pro.json`** (si existe config)

5. **`README.md`**
   - Instrucciones de importación
   - Cómo configurar autenticación
   - Variables disponibles

#### Paso 4: Mostrar resultado

```
📁 06_Documentacion/Postman/
   ├── Comillas.MiApi_collection.json     (X requests)
   ├── Comillas.MiApi_environment_dev.json
   ├── Comillas.MiApi_environment_pre.json
   ├── Comillas.MiApi_environment_pro.json
   └── README.md

═══════════════════════════════════════════════════════════════

📋 PRÓXIMOS PASOS:

1. Abrir Postman
2. Import → File → Seleccionar collection.json
3. Import → File → Seleccionar environment deseado
4. Seleccionar environment en dropdown superior derecho
5. Configurar variable {{token}} con tu JWT

✅ Colección lista para usar
```

---

### Al ejecutar `/postman collection`

Solo genera la colección JSON, sin environments.

### Al ejecutar `/postman environment`

Solo genera los archivos de environment.

Preguntar si no se detectan URLs:

```
No se detectaron URLs en appsettings.

Introduce las URLs para cada entorno:

Development URL: [input]
Preproducción URL (opcional): [input]
Producción URL (opcional): [input]
```

### Al ejecutar `/postman tests`

Buscar colección existente en `06_Documentacion/Postman/` y añadir/actualizar tests.

### Al ejecutar `/postman swagger [url]`

```
/postman swagger
/postman swagger https://localhost:7001/swagger/v1/swagger.json
```

1. Si no se proporciona URL, buscar en:
   - `wwwroot/swagger.json`
   - `/swagger/v1/swagger.json` (hacer request si servidor corriendo)

2. Parsear OpenAPI spec
3. Generar colección con toda la info del swagger
4. Incluir ejemplos de los schemas

---

## Estructura de Colección Generada

```json
{
  "info": {
    "name": "Proyecto API",
    "description": "Documentación generada por Claude Code STIC"
  },
  "auth": {
    "type": "bearer",
    "bearer": [{ "key": "token", "value": "{{token}}" }]
  },
  "item": [
    {
      "name": "Becas",
      "item": [
        {
          "name": "Obtener todas las becas",
          "request": {
            "method": "GET",
            "url": "{{baseUrl}}/api/becas"
          },
          "response": []
        }
      ]
    }
  ]
}
```

---

## Tests Automáticos por Tipo

| Método | Tests incluidos |
|--------|-----------------|
| **GET lista** | Status 200, es array, tiempo respuesta |
| **GET por ID** | Status 200/404, tiene ID, formato error |
| **POST** | Status 201, Location header, guarda ID |
| **PUT** | Status 200/204, tiempo respuesta |
| **DELETE** | Status 204, body vacío |
| **Auth** | Guarda token en environment |

---

## Variables de Environment

| Variable | Descripción |
|----------|-------------|
| `baseUrl` | URL base del API |
| `token` | JWT o access token |
| `refreshToken` | Token de refresco |
| `lastCreatedId` | ID del último recurso creado |
| `testUserId` | Usuario de prueba |
| `testEmail` | Email de prueba |

---

## Integración con CI/CD

Incluir en README.md instrucciones para Newman:

```bash
# Ejecutar tests con Newman
npm install -g newman
newman run collection.json -e environment_dev.json
```

---

## Skill asociado

Este comando activa el skill `postman-collection` que contiene:
- Plantillas de colección
- Plantillas de environment
- Plantillas de tests
- Lógica de detección de endpoints

---

*Comando v1.0.0 - Generador de Colecciones Postman*
