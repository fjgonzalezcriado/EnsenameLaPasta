# AI Trading Simulator - ASP.NET Core MVC

## Objetivo del proyecto

El proyecto es de carácter personal.

Construir una aplicación web MVC en ASP.NET Core 10 orientada a:

- Simulación de trading algorítmico
- Backtesting de estrategias
- Gestión de señales de compra/venta
- Simulación de portfolio
- Visualización de métricas
- Preparación futura para integración IA/ML

La aplicación NO operará inicialmente con dinero real.

El objetivo de la primera entrega es:
- disponer de un simulador completamente funcional
- arquitectura limpia
- persistencia ligera local
- base sólida para evolucionar posteriormente

---

# Stack tecnológico

## Backend

- ASP.NET Core MVC (.NET 10)
- C#
- Entity Framework Core
- SQLite

## Frontend

- Razor Views
- Bootstrap 5
- Chart.js
- jQuery (solo si es necesario)

## Base de datos

SQLite local.

La base de datos debe:
- crearse automáticamente
- usar migrations EF Core
- almacenarse en `/App_Data/trading.db`

NO utilizar SQL Server.

---

# Arquitectura

El proyecto seguirá una arquitectura modular inspirada en Clean Architecture.

## Capas

### Web

Responsabilidades:
- Controllers
- Views
- ViewModels
- configuración web
- autenticación futura

### Application

Responsabilidades:
- Casos de uso
- Servicios
- Interfaces
- Lógica de negocio

NO debe contener dependencias de infraestructura.

### Infrastructure

Responsabilidades:
- EF Core
- acceso a datos
- APIs externas
- repositorios
- persistencia

### Domain

Responsabilidades:
- entidades
- enums
- reglas de negocio
- value objects

NO debe depender de ninguna otra capa.

---

# Primera entrega funcional

## Objetivo MVP

Implementar un simulador simple de trading.

---

# Funcionalidades iniciales

## Dashboard

Página principal con:

- balance virtual
- pnl total
- operaciones abiertas
- operaciones cerradas
- gráfico de evolución

---

## Simulación de mercado

Generar datos fake de mercado:

- símbolo
- precio
- timestamp
- volumen

Inicialmente:
- datos aleatorios controlados
- sin conexión a APIs reales

---

## Estrategia básica

Implementar estrategia simple:

### Moving Average Crossover

Comprar:
- MA corta > MA larga

Vender:
- MA corta < MA larga

La estrategia debe ejecutarse automáticamente.

---

## Simulador de órdenes

Permitir:

- Buy
- Sell
- Close Position

Cada operación debe persistirse.

---

## Portfolio

Mostrar:

- capital inicial
- capital actual
- pnl
- drawdown
- winrate

---

# Entidades principales

## Trade

```csharp
public class Trade
{
    public Guid Id { get; set; }

    public string Symbol { get; set; } = string.Empty;

    public decimal EntryPrice { get; set; }

    public decimal? ExitPrice { get; set; }

    public decimal Quantity { get; set; }

    public TradeStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ClosedAt { get; set; }
}
```

## MarketTick

```csharp
public class MarketTick
{
    public Guid Id { get; set; }

    public string Symbol { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public decimal Volume { get; set; }

    public DateTime Timestamp { get; set; }
}
```

---

# Reglas de arquitectura

## Reglas obligatorias

- NO usar lógica en Views
- Controllers delgados
- Toda lógica en Application
- Async/await obligatorio
- Dependency Injection en todo
- Configuración mediante Options Pattern
- Repositorios desacoplados
- Evitar código estático

---

# Persistencia

## EF Core

Usar:
- Code First
- Migrations

Naming:
- tablas en singular
- claves GUID

---

# Logging

Implementar logging desde inicio:

- ILogger<T>
- logs informativos
- logs de errores
- logs de simulación

---

# Configuración

Usar:

```json
appsettings.json
```

Configurar:

- capital inicial
- frecuencia simulación
- símbolos simulados
- parámetros estrategia

---

# Background Services

Usar:

```csharp
BackgroundService
```

Para:

- generar ticks de mercado
- ejecutar estrategias
- actualizar portfolio

---

# UI

La interfaz debe ser:

- limpia
- minimalista
- orientada a dashboard
- responsive

---

# Métricas

Mostrar:

- operaciones ganadas
- operaciones perdidas
- profit factor
- drawdown
- pnl acumulado

---

# Roadmap futuro

## Fase 2

- conexión APIs reales
- Yahoo Finance
- Binance
- AlphaVantage

## Fase 3

- IA predictiva
- ML.NET
- ONNX
- modelos entrenados

## Fase 4

- paper trading real
- ejecución automática

---

# Buenas prácticas

## Obligatorias

- SOLID
- Clean Code
- CQRS opcional
- Unit Testing
- separación estricta de responsabilidades

---

# Restricciones

NO implementar inicialmente:

- autenticación
- microservicios
- docker
- kubernetes
- mensajería distribuida
- trading real

El foco es:
- simulación
- arquitectura sólida
- mantenibilidad
- capacidad de evolución

---

# Prioridad máxima

La prioridad principal es:

1. estabilidad
2. simplicidad
3. mantenibilidad
4. extensibilidad

NO sobreingenierizar.

---

# Resultado esperado primera entrega

Aplicación MVC capaz de:

- simular mercado
- ejecutar estrategia automática
- abrir/cerrar trades
- persistir operaciones
- mostrar métricas
- visualizar resultados

Todo funcionando localmente usando SQLite.