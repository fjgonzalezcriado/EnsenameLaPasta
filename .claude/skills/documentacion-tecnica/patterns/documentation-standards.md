# Estandares de Documentacion Comillas

> Skill: documentacion-tecnica | Version: 3.1.0

Convenciones para documentacion tecnica en proyectos de la Universidad Pontificia Comillas.

---

## Nomenclatura de Archivos

- Mayusculas con guiones bajos: `GUIA_DESPLIEGUE.md`
- Templates: `NOMBRE.md.template`
- Prefijo por tipo: README, CHANGELOG, ADR-XXX

## Idioma

- Documentacion tecnica: **Espanol**
- Codigo y comentarios: **Ingles** (excepto dominio de negocio)
- Commits: Espanol o ingles (consistente por proyecto)

## Secciones Obligatorias por Tipo

| Documento | Secciones |
|-----------|-----------|
| README | Descripcion, Prerequisitos, Instalacion, Uso |
| ADR | Estado, Contexto, Decision, Consecuencias |
| API Doc | Autenticacion, Endpoints, Ejemplos, Errores |
| Changelog | Versionado SemVer, categorias Added/Changed/Fixed |

## Formato Markdown

- Encabezados: maximo 3 niveles (H1, H2, H3)
- Tablas: para datos estructurados
- Bloques de codigo: siempre con lenguaje (```csharp)
- Sin emojis en documentos formales

---

*Pattern v3.1.0*
