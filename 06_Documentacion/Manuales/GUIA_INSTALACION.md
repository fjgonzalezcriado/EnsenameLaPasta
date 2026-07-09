# Guía de Instalación — Enseñame la Pasta

> Cómo poner en marcha la aplicación en un equipo nuevo. Es una **app web local**
> (ASP.NET Core MVC · .NET 10): se ejecuta en tu propia máquina, **no hay servidor que desplegar**.

---

## Información del Documento

| Campo | Valor |
|-------|-------|
| **Aplicación** | Enseñame la Pasta |
| **Tipo** | Aplicación web local (uso personal) |
| **Framework** | .NET 10 / C# 13 |
| **Repositorio** | `github.com/fjgonzalezcriado/EnsenameLaPasta` (privado) |
| **Fecha** | 2026-07-08 |

---

## 1. Requisitos previos (una sola vez)

| Requisito | Cómo obtenerlo / verificar |
|---|---|
| **.NET 10 SDK** | Descargar de [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download). Verificar: `dotnet --version` (debe empezar por `10.`). |
| **Git** | [git-scm.com](https://git-scm.com). Verificar: `git --version`. |
| **Acceso al repositorio** | Es **privado**: el propietario debe añadirte como *collaborator* en GitHub (*Settings → Collaborators*). |
| **Conexión a Internet** | Los precios y los tipos de cambio se obtienen de **Yahoo Finance** en tiempo real. |
| *(Opcional)* **Visual Studio 2022 17.10+** | Solo si prefieres IDE en vez de línea de comandos (soporta el formato `.slnx`). |

---

## 2. Instalación y arranque (línea de comandos)

```bash
# 1) Clonar el repositorio
git clone https://github.com/fjgonzalezcriado/EnsenameLaPasta.git
cd EnsenameLaPasta

# 2) Confiar en el certificado de desarrollo HTTPS (solo la primera vez)
dotnet dev-certs https --trust

# 3) Arrancar la aplicación web
cd 03_Desarrollo/EnsenameLaPasta.Web
dotnet run
```

Cuando la consola muestre `Now listening on: ...`, abre en el navegador:

- **https://localhost:7299**  (o `http://localhost:5177`)

Para parar la app: **Ctrl+C** en la consola.

### Alternativa con Visual Studio

1. Abre `03_Desarrollo/EnsenameLaPasta.slnx`.
2. Proyecto de inicio: **`EnsenameLaPasta.Web`**.
3. Pulsa **F5** (o Ctrl+F5 sin depurar).

---

## 3. Qué ocurre en el primer arranque

- Se crea automáticamente la base de datos **SQLite** (`App_Data/trading.db`) y se **aplican las
  migraciones** al iniciar (no hay que ejecutar nada de Entity Framework a mano).
- La aplicación arranca con la **watchlist sembrada** (`HY9H.F`) y la **cartera vacía**: la base de
  datos del propietario **no** está en el repositorio (está excluida por `.gitignore`). Cada persona
  parte de cero y registra sus propias posiciones, caja y dividendos.
- **Yahoo Finance funciona sin API key.** Los proveedores Twelve Data y Alpha Vantage son opcionales
  y requieren clave (se configuran con *user-secrets*, nunca en el repositorio) — no hacen falta para
  usar la aplicación.

---

## 4. Configuración opcional (proveedores con API key)

Solo si quieres usar Twelve Data o Alpha Vantage además de Yahoo. Las claves se guardan **fuera del
repositorio** con *user-secrets*:

```bash
cd 03_Desarrollo/EnsenameLaPasta.Web
dotnet user-secrets init
dotnet user-secrets set "MarketData:TwelveData:ApiKey"   "TU_CLAVE"
dotnet user-secrets set "MarketData:AlphaVantage:ApiKey" "TU_CLAVE"
```

En la interfaz, el selector de proveedor mostrará «⚠ sin API key» si falta la clave del proveedor
elegido. Con Yahoo (por defecto) no es necesario nada de esto.

---

## 5. Avisos importantes

- ⚠️ **NO ejecutes `03_Desarrollo/arranque.ps1`.** Es el *bootstrap* de la plantilla STIC.IA y
  contacta con servidores de la Universidad; este proyecto está en **modo "go-dark"** (personal,
  desconectado de Comillas). Para instalarlo basta con **clonar + `dotnet run`**.
- Tras cambios de interfaz, recarga el navegador con **Ctrl+F5** (evita CSS/JS cacheados).
- La aplicación **necesita Internet** (Yahoo Finance). Sin conexión no puede refrescar precios ni
  tipos de cambio.
- La cartera de cada persona es **local a su equipo** (su propio `trading.db`); no se comparte por el
  repositorio.

---

## 6. Solución de problemas

| Problema | Solución |
|---|---|
| `dotnet` no se reconoce | Reinstala el **.NET 10 SDK** y reabre la terminal (para refrescar el `PATH`). |
| El navegador avisa de certificado no confiable | Ejecuta `dotnet dev-certs https --trust` y reinicia el navegador. |
| «Failed to bind to address … in use» | El puerto 7299/5177 está ocupado. Cierra la instancia previa o cambia el puerto en `Properties/launchSettings.json`. |
| No aparecen precios | Comprueba la conexión a Internet; con un proveedor distinto de Yahoo, revisa que la API key esté configurada. |
| Errores de compilación al clonar | Verifica que tienes el **.NET 10 SDK** (no solo el runtime): `dotnet --list-sdks`. |

---

## 7. Uso de la aplicación

Una vez arrancada, consulta el **[Manual de Usuario](MANUAL_USUARIO.md)** y el
**[Glosario de términos](GLOSARIO.md)** para aprender a usar el panel (watchlist, posiciones, caja,
dividendos, gráficos, métricas, etc.).

---

*Guía de instalación de Enseñame la Pasta. Proyecto personal (.NET 10, SQLite, feed Yahoo Finance).*
