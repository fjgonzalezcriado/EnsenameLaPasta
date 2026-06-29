# Structured Logging - Best Practices

> Guia de logging estructurado para proyectos Comillas con cumplimiento RGPD.

---

## Principio: Templates con Parametros

```csharp
// CORRECTO - Structured logging con templates
_logger.LogInformation("Beca {BecaId} creada por usuario {UsuarioId}", beca.Id, userId);
_logger.LogWarning("Intento de acceso denegado a beca {BecaId} desde IP {IpAddress}", becaId, ip);
_logger.LogError(ex, "Error al procesar beca {BecaId}", becaId);

// INCORRECTO - Concatenacion de strings (no estructurado)
_logger.LogInformation("Beca " + beca.Id + " creada por " + userId);  // MAL
_logger.LogInformation($"Beca {beca.Id} creada por {userId}");          // MAL
```

**Motivo**: Los templates permiten que las herramientas de logging (App Insights, Seq) indexen y busquen por propiedades individuales. La concatenacion pierde esta capacidad.

---

## Que Loguear por Nivel

| Nivel | Cuando usar | Ejemplo |
|-------|-------------|---------|
| **Debug** | Detalle tecnico, solo desarrollo | `"Query ejecutada: {Query} en {ElapsedMs}ms"` |
| **Information** | Operaciones normales importantes | `"Beca {BecaId} creada por {UsuarioId}"` |
| **Warning** | Situaciones anomalas no criticas | `"Timeout en intento {Intento}/{MaxIntentos} para {Servicio}"` |
| **Error** | Errores que requieren atencion | `"Error al procesar beca {BecaId}: {ErrorMessage}"` |
| **Fatal** | Errores que impiden funcionamiento | `"No se puede conectar a SQL Server: {ConnectionError}"` |

---

## Que NO Loguear (RGPD)

### Datos Prohibidos

| Dato | Ejemplo prohibido | Alternativa correcta |
|------|-------------------|---------------------|
| Contrasenas | `"Login con password {Password}"` | `"Login para usuario {UsuarioId}"` |
| Tokens | `"Token: {BearerToken}"` | `"Token validado para {UsuarioId}"` |
| DNI/NIE | `"DNI: {Dni}"` | `"Estudiante {EstudianteId} verificado"` |
| Email personal | `"Email: {Email}"` | `"Notificacion enviada a usuario {UsuarioId}"` |
| Telefono | `"Tel: {Telefono}"` | No loguear |
| Tarjeta credito | `"Tarjeta: {NumeroTarjeta}"` | `"Pago procesado {PagoId}"` |
| Datos salud | `"Diagnostico: {Diagnostico}"` | No loguear |
| Direccion | `"Direccion: {Domicilio}"` | No loguear |

### Regla General

```
LOGUEAR: IDs internos, codigos, estados, metricas, errores tecnicos
NO LOGUEAR: datos personales, credenciales, datos sensibles
```

---

## Correlacion de Trazas

```csharp
// El TraceId se propaga automaticamente con OpenTelemetry
// Serilog lo captura via enricher FromLogContext

// En el log aparecera:
// [2026-01-20 10:30:00] [INF] (BecaService) TraceId=abc123 Beca 42 creada
// [2026-01-20 10:30:01] [INF] (EmailService) TraceId=abc123 Email enviado para beca 42
// -> Misma traza, facil de correlacionar

// Anadir contexto adicional manualmente
using (LogContext.PushProperty("BecaId", becaId))
using (LogContext.PushProperty("OperacionId", operacionId))
{
    _logger.LogInformation("Iniciando procesamiento de beca");
    await ProcesarAsync(becaId);
    _logger.LogInformation("Procesamiento completado");
}
// Ambos logs tendran BecaId y OperacionId como propiedades
```

---

## Patrones de Logging por Capa

### Controllers

```csharp
// Loguear entrada/salida de endpoints criticos
[HttpPost]
public async Task<ActionResult<BecaDto>> Create(CreateBecaRequest request, CancellationToken ct)
{
    _logger.LogInformation("Creando beca {BecaCodigo} por {Usuario}",
        request.Codigo, User.Identity?.Name);

    var result = await _service.CreateAsync(request, ct);

    _logger.LogInformation("Beca {BecaId} creada exitosamente", result.Id);

    return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
}
```

### Services

```csharp
// Loguear operaciones de negocio significativas
public async Task<BecaDto> ProcesarAsync(int becaId, CancellationToken ct)
{
    _logger.LogDebug("Procesando beca {BecaId}", becaId);

    var beca = await _repository.GetByIdAsync(becaId, ct);
    if (beca is null)
    {
        _logger.LogWarning("Beca {BecaId} no encontrada", becaId);
        throw new NotFoundException("Beca", becaId);
    }

    try
    {
        beca.Publicar();
        await _repository.UpdateAsync(beca, ct);
        _logger.LogInformation("Beca {BecaId} publicada. Estado: {Estado}", becaId, beca.Estado);
    }
    catch (DomainException ex)
    {
        _logger.LogWarning(ex, "No se pudo publicar beca {BecaId}: {Motivo}", becaId, ex.Message);
        throw;
    }

    return MapToDto(beca);
}
```

### Infrastructure (llamadas externas)

```csharp
// Loguear integraciones con sistemas externos
public async Task<ExternalResponse> CallExternalServiceAsync(string endpoint, CancellationToken ct)
{
    _logger.LogDebug("Llamando a servicio externo {Endpoint}", endpoint);
    var sw = Stopwatch.StartNew();

    try
    {
        var response = await _httpClient.GetAsync(endpoint, ct);
        sw.Stop();

        _logger.LogInformation(
            "Respuesta de {Endpoint}: {StatusCode} en {ElapsedMs}ms",
            endpoint, (int)response.StatusCode, sw.ElapsedMilliseconds);

        return await response.Content.ReadFromJsonAsync<ExternalResponse>(ct);
    }
    catch (HttpRequestException ex)
    {
        sw.Stop();
        _logger.LogError(ex,
            "Error llamando a {Endpoint} tras {ElapsedMs}ms: {ErrorMessage}",
            endpoint, sw.ElapsedMilliseconds, ex.Message);
        throw;
    }
}
```

---

## Anti-Patrones

```csharp
// 1. NO loguear dentro de bucles sin control
foreach (var item in items)  // 10,000 items!
{
    _logger.LogInformation("Procesando {ItemId}", item.Id);  // MAL: 10K logs
}
// MEJOR: Loguear antes y despues con conteo
_logger.LogInformation("Procesando {Count} items", items.Count);

// 2. NO usar ToString() en objetos complejos
_logger.LogInformation("Beca: {Beca}", beca.ToString());  // MAL
_logger.LogInformation("Beca {BecaId} estado {Estado}", beca.Id, beca.Estado);  // BIEN

// 3. NO catch vacio o sin logging
catch (Exception) { }  // MAL: error silencioso
catch (Exception ex) { _logger.LogError(ex, "Error en..."); throw; }  // BIEN

// 4. NO loguear y luego throw new sin inner exception
catch (Exception ex)
{
    _logger.LogError(ex, "Error");
    throw new Exception("Error");  // MAL: pierde stack trace
    throw;  // BIEN: preserva stack trace
}
```

---

## Checklist de Logging

- [ ] Usar templates con parametros (no concatenacion ni interpolacion)
- [ ] No loguear datos personales ni credenciales (RGPD)
- [ ] Loguear IDs internos para trazabilidad
- [ ] Nivel apropiado (Debug->Info->Warning->Error->Fatal)
- [ ] Incluir exception como primer parametro en LogError
- [ ] No loguear dentro de bucles de alto volumen
- [ ] Configurar niveles por namespace en appsettings.json
- [ ] Correlacion de trazas con TraceId/OperacionId

---

*Pattern structured-logging v3.7.0*
