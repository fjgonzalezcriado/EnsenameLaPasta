# Checklist - Review de Base de Datos

> Skill: analisis-arquitectura | Version: 3.1.0

---

## Diseno de Schema

- [ ] Normalizacion adecuada (3NF minimo)
- [ ] Nombres de tablas en plural (Becas, Estudiantes)
- [ ] Nombres de columnas claros y consistentes
- [ ] Primary keys definidas (Id int IDENTITY)
- [ ] Foreign keys con constraints
- [ ] Soft delete donde aplica (IsDeleted + DeletedAt)

## Indices

- [ ] Indice en cada foreign key
- [ ] Indices cubrientes para queries frecuentes
- [ ] No hay indices duplicados
- [ ] Indice filtrado para soft delete ([IsDeleted] = 0)

## Rendimiento

- [ ] No hay SELECT * en produccion
- [ ] Queries con AsNoTracking para solo lectura
- [ ] Paginacion server-side (Skip/Take)
- [ ] N+1 detectados y resueltos (Include/ThenInclude)
- [ ] Queries complejas analizadas con execution plan

## Migraciones

- [ ] Migraciones idempotentes
- [ ] Rollback posible para cada migracion
- [ ] No hay datos de seed en migraciones de produccion
- [ ] Migraciones versionadas y en control de fuentes

## Seguridad

- [ ] Principio de minimo privilegio (roles de BD)
- [ ] Connection string sin password en appsettings
- [ ] TDE habilitado en Azure SQL
- [ ] Backup strategy definida

## Monitoring

- [ ] Query Store habilitado
- [ ] Alertas de DTU/vCores altos
- [ ] Deadlock monitoring configurado

---

*Checklist v3.1.0*
