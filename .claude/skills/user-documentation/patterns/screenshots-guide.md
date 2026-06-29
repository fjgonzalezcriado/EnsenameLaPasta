# Guia de Capturas de Pantalla

> Skill: user-documentation | Version: 3.1.0

Buenas practicas para capturas en documentacion de usuario.

---

## Estandares

| Aspecto | Valor |
|---------|-------|
| Resolucion minima | 1280x720 |
| Formato | PNG |
| Anotaciones | Flechas rojas, recuadros |
| DPI | 144 (retina) |

## Nomenclatura

`[seccion]-[funcionalidad]_[paso].png`

Ejemplo: `becas-listado_01.png`, `becas-crear_02.png`

## Carpeta

`screenshots/` en la raiz del proyecto de documentacion.

## Actualizacion

Cuando cambie la UI:
1. Identificar screenshots afectados
2. Regenerar con la nueva UI
3. Mantener el mismo nombre de archivo
4. Commit con mensaje descriptivo

## Accesibilidad

- Siempre incluir alt text descriptivo
- No depender solo de la imagen para transmitir informacion
- Incluir texto explicativo junto a cada captura

---

*Pattern v3.1.0*
