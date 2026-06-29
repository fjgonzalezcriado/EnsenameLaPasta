# Checklist - Review de Microservicios

> Skill: analisis-arquitectura | Version: 3.1.0

---

## Limites de Servicio

- [ ] Cada servicio tiene una responsabilidad clara y acotada
- [ ] No hay dependencias circulares entre servicios
- [ ] Cada servicio puede desplegarse independientemente

## Comunicacion

- [ ] Comunicacion sincrona (HTTP/gRPC) solo cuando es necesario
- [ ] Comunicacion asincrona (Service Bus) para eventos
- [ ] Contratos de API versionados
- [ ] Circuit breaker implementado en llamadas entre servicios

## Datos

- [ ] Cada servicio tiene su propia base de datos
- [ ] No hay acceso directo a BD de otro servicio
- [ ] Consistencia eventual aceptada donde aplica
- [ ] Saga pattern para transacciones distribuidas

## Resiliencia

- [ ] Health checks por servicio
- [ ] Retry con backoff exponencial
- [ ] Timeouts configurados
- [ ] Graceful shutdown implementado

## Observabilidad

- [ ] Logging centralizado (correlacion de traza)
- [ ] Metricas por servicio
- [ ] Distributed tracing (OpenTelemetry)
- [ ] Dashboard de monitorizacion

## Seguridad

- [ ] Autenticacion servicio-a-servicio (Managed Identity)
- [ ] No hay secretos en codigo o config
- [ ] mTLS entre servicios (si aplica)

---

*Checklist v3.1.0*
