# Colores Corporativos Comillas

> Paleta oficial de colores para aplicaciones web de la Universidad Pontificia Comillas.

---

## Colores Principales

### Azul Comillas (Principal)
```css
--comillas-azul: #003366;
```
- **Uso**: Header, sidebar, botones primarios, títulos principales
- **RGB**: 0, 51, 102
- **HSL**: 210°, 100%, 20%

### Azul Claro (Secundario)
```css
--comillas-azul-claro: #0066CC;
```
- **Uso**: Enlaces, hover states, iconos, acentos
- **RGB**: 0, 102, 204
- **HSL**: 210°, 100%, 40%

### Azul Hover
```css
--comillas-azul-hover: #004D99;
```
- **Uso**: Estados hover de botones primarios
- **RGB**: 0, 77, 153
- **HSL**: 210°, 100%, 30%

---

## Colores Neutros

### Gris Oscuro (Texto)
```css
--comillas-gris-oscuro: #333333;
```
- **Uso**: Texto principal, párrafos
- **RGB**: 51, 51, 51
- **Contraste con blanco**: 12.6:1

### Gris Medio
```css
--comillas-gris-medio: #666666;
```
- **Uso**: Texto secundario, placeholders
- **RGB**: 102, 102, 102
- **Contraste con blanco**: 5.7:1

### Gris Claro (Fondo)
```css
--comillas-gris-claro: #F5F5F5;
```
- **Uso**: Fondos secundarios, cards, separadores
- **RGB**: 245, 245, 245

### Blanco
```css
--comillas-blanco: #FFFFFF;
```
- **Uso**: Fondo principal, texto sobre azul
- **RGB**: 255, 255, 255

---

## Colores de Estado

### Error / Alerta Crítica
```css
--comillas-error: #CC0000;
```
- **Uso**: Mensajes de error, validaciones fallidas, alertas críticas
- **RGB**: 204, 0, 0
- **Contraste con blanco**: 6.5:1

### Éxito
```css
--comillas-exito: #28A745;
```
- **Uso**: Confirmaciones, operaciones exitosas
- **RGB**: 40, 167, 69
- **Contraste con blanco**: 4.5:1

### Advertencia
```css
--comillas-warning: #FFC107;
```
- **Uso**: Avisos, información importante
- **RGB**: 255, 193, 7
- **Nota**: Usar texto oscuro (#333) sobre este fondo

### Información
```css
--comillas-info: #17A2B8;
```
- **Uso**: Información contextual, tooltips
- **RGB**: 23, 162, 184
- **Contraste con blanco**: 4.5:1

---

## Variables CSS Completas

```css
:root {
    /* Principales */
    --comillas-azul: #003366;
    --comillas-azul-claro: #0066CC;
    --comillas-azul-hover: #004D99;
    --comillas-azul-light: #E6F0FF;

    /* Neutros */
    --comillas-gris-oscuro: #333333;
    --comillas-gris-medio: #666666;
    --comillas-gris-claro: #F5F5F5;
    --comillas-blanco: #FFFFFF;
    --comillas-borde: #DDDDDD;

    /* Estados */
    --comillas-error: #CC0000;
    --comillas-error-bg: #FFE6E6;
    --comillas-exito: #28A745;
    --comillas-exito-bg: #E6F4EA;
    --comillas-warning: #FFC107;
    --comillas-warning-bg: #FFF8E1;
    --comillas-info: #17A2B8;
    --comillas-info-bg: #E1F5FE;

    /* Sombras */
    --comillas-sombra-sm: 0 1px 2px rgba(0, 0, 0, 0.1);
    --comillas-sombra-md: 0 4px 6px rgba(0, 0, 0, 0.1);
    --comillas-sombra-lg: 0 10px 15px rgba(0, 0, 0, 0.1);
}
```

---

## Combinaciones de Contraste

### Texto sobre fondos (WCAG AA - 4.5:1 mínimo)

| Fondo | Color Texto | Ratio | Estado |
|-------|-------------|-------|--------|
| Blanco (#FFF) | Azul (#003366) | 12.6:1 | OK |
| Blanco (#FFF) | Gris oscuro (#333) | 12.6:1 | OK |
| Blanco (#FFF) | Gris medio (#666) | 5.7:1 | OK |
| Azul (#003366) | Blanco (#FFF) | 12.6:1 | OK |
| Gris claro (#F5F5F5) | Azul (#003366) | 11.5:1 | OK |
| Gris claro (#F5F5F5) | Gris oscuro (#333) | 11.5:1 | OK |
| Warning (#FFC107) | Gris oscuro (#333) | 8.5:1 | OK |

### Colores que NO cumplen contraste

| Combinación | Ratio | Problema |
|-------------|-------|----------|
| Blanco + Azul claro (#0066CC) | 4.1:1 | Usar solo para elementos grandes |
| Gris claro + Gris medio | 3.1:1 | No usar para texto |

---

## Uso en Componentes

### Botones

```css
/* Botón primario */
.btn-comillas-primary {
    background-color: var(--comillas-azul);
    color: var(--comillas-blanco);
    border: none;
}

.btn-comillas-primary:hover {
    background-color: var(--comillas-azul-hover);
}

/* Botón secundario */
.btn-comillas-secondary {
    background-color: var(--comillas-blanco);
    color: var(--comillas-azul);
    border: 2px solid var(--comillas-azul);
}
```

### Enlaces

```css
a {
    color: var(--comillas-azul-claro);
    text-decoration: none;
}

a:hover {
    color: var(--comillas-azul);
    text-decoration: underline;
}

a:focus {
    outline: 2px solid var(--comillas-azul-claro);
    outline-offset: 2px;
}
```

### Alertas

```css
.alert-error {
    background-color: var(--comillas-error-bg);
    border-left: 4px solid var(--comillas-error);
    color: var(--comillas-gris-oscuro);
}

.alert-success {
    background-color: var(--comillas-exito-bg);
    border-left: 4px solid var(--comillas-exito);
}
```

---

## Reglas de Uso

1. **Nunca usar** colores fuera de esta paleta sin aprobación
2. **Siempre usar** variables CSS, no valores hardcodeados
3. **Verificar contraste** antes de crear nuevas combinaciones
4. **El azul Comillas** es el color principal, usarlo con moderación
5. **Evitar** texto gris medio sobre fondos claros para contenido importante

---

*Última actualización: Enero 2026*
