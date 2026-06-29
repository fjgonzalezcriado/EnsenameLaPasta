# Responsive Design - Comillas

> Guía de diseño responsive para aplicaciones web de Comillas.

---

## Breakpoints

```css
:root {
    --breakpoint-mobile: 768px;
    --breakpoint-tablet: 1200px;
}

/* Mobile: < 768px */
@media (max-width: 767px) { }

/* Tablet: 768px - 1199px */
@media (min-width: 768px) and (max-width: 1199px) { }

/* Desktop: >= 1200px */
@media (min-width: 1200px) { }
```

---

## Comportamiento por Dispositivo

### Desktop (>= 1200px)

| Elemento | Comportamiento |
|----------|----------------|
| **Header** | Fijo, 60px altura |
| **Sidebar** | Visible expandido (250px) |
| **Contenido** | margin-left: 250px |
| **Footer** | Fijo en bottom |
| **Grids** | 12 columnas disponibles |
| **Tablas** | Completas, scroll horizontal si excede |

### Tablet (768px - 1199px)

| Elemento | Comportamiento |
|----------|----------------|
| **Header** | Fijo, 60px altura |
| **Sidebar** | Colapsado (60px), expandible al hover/click |
| **Contenido** | margin-left: 60px |
| **Footer** | Fijo en bottom |
| **Grids** | 6-8 columnas típico |
| **Tablas** | Scroll horizontal habilitado |

### Mobile (< 768px)

| Elemento | Comportamiento |
|----------|----------------|
| **Header** | Sticky, hamburger menu visible |
| **Sidebar** | Oculto, overlay al abrir hamburger |
| **Contenido** | 100% ancho, padding reducido (16px) |
| **Footer** | Estático, no fijo |
| **Grids** | 1-2 columnas máximo |
| **Tablas** | Cards o scroll horizontal |

---

## CSS Media Queries

### Mobile First (Recomendado)

```css
/* Base: Mobile */
.container {
    padding: 16px;
    width: 100%;
}

.sidebar {
    display: none;
    position: fixed;
    top: 60px;
    left: 0;
    width: 280px;
    height: calc(100vh - 60px);
    z-index: 900;
    transform: translateX(-100%);
    transition: transform 0.3s ease;
}

.sidebar.open {
    transform: translateX(0);
}

/* Tablet */
@media (min-width: 768px) {
    .container {
        padding: 20px;
    }

    .sidebar {
        display: block;
        width: 60px;
        transform: translateX(0);
    }

    .sidebar:hover,
    .sidebar.expanded {
        width: 250px;
    }

    .content-wrapper {
        margin-left: 60px;
    }
}

/* Desktop */
@media (min-width: 1200px) {
    .container {
        padding: 24px;
        max-width: 1400px;
        margin: 0 auto;
    }

    .sidebar {
        width: 250px;
    }

    .content-wrapper {
        margin-left: 250px;
    }
}
```

---

## Grid System

### CSS Grid Responsive

```css
.grid {
    display: grid;
    gap: 16px;
    grid-template-columns: 1fr;
}

@media (min-width: 768px) {
    .grid {
        grid-template-columns: repeat(2, 1fr);
    }

    .grid-3 {
        grid-template-columns: repeat(3, 1fr);
    }
}

@media (min-width: 1200px) {
    .grid {
        gap: 24px;
    }

    .grid-3 {
        grid-template-columns: repeat(3, 1fr);
    }

    .grid-4 {
        grid-template-columns: repeat(4, 1fr);
    }
}
```

### Columnas Flexibles

```css
.row {
    display: flex;
    flex-wrap: wrap;
    margin: -8px;
}

.col {
    padding: 8px;
    flex: 0 0 100%;
    max-width: 100%;
}

@media (min-width: 768px) {
    .col-md-6 { flex: 0 0 50%; max-width: 50%; }
    .col-md-4 { flex: 0 0 33.333%; max-width: 33.333%; }
    .col-md-3 { flex: 0 0 25%; max-width: 25%; }
}

@media (min-width: 1200px) {
    .col-lg-6 { flex: 0 0 50%; max-width: 50%; }
    .col-lg-4 { flex: 0 0 33.333%; max-width: 33.333%; }
    .col-lg-3 { flex: 0 0 25%; max-width: 25%; }
    .col-lg-2 { flex: 0 0 16.666%; max-width: 16.666%; }
}
```

---

## Tablas Responsive

### Opción 1: Scroll Horizontal

```css
.table-responsive {
    overflow-x: auto;
    -webkit-overflow-scrolling: touch;
}

.table-responsive table {
    min-width: 600px;
}
```

### Opción 2: Convertir a Cards (Mobile)

```css
@media (max-width: 767px) {
    .table-cards thead {
        display: none;
    }

    .table-cards tr {
        display: block;
        margin-bottom: 16px;
        border: 1px solid var(--comillas-borde);
        border-radius: 8px;
        padding: 12px;
    }

    .table-cards td {
        display: flex;
        justify-content: space-between;
        padding: 8px 0;
        border-bottom: 1px solid var(--comillas-borde);
    }

    .table-cards td::before {
        content: attr(data-label);
        font-weight: 600;
        color: var(--comillas-gris-oscuro);
    }

    .table-cards td:last-child {
        border-bottom: none;
    }
}
```

---

## Navegación Móvil

### Hamburger Menu

```css
.hamburger {
    display: none;
    background: none;
    border: none;
    padding: 12px;
    cursor: pointer;
}

.hamburger-line {
    display: block;
    width: 24px;
    height: 2px;
    background: white;
    margin: 5px 0;
    transition: transform 0.3s;
}

@media (max-width: 767px) {
    .hamburger {
        display: block;
    }

    .hamburger.open .hamburger-line:nth-child(1) {
        transform: rotate(45deg) translate(5px, 5px);
    }

    .hamburger.open .hamburger-line:nth-child(2) {
        opacity: 0;
    }

    .hamburger.open .hamburger-line:nth-child(3) {
        transform: rotate(-45deg) translate(5px, -5px);
    }
}
```

### Overlay

```css
.sidebar-overlay {
    display: none;
    position: fixed;
    top: 60px;
    left: 0;
    right: 0;
    bottom: 0;
    background: rgba(0, 0, 0, 0.5);
    z-index: 850;
}

@media (max-width: 767px) {
    .sidebar.open ~ .sidebar-overlay {
        display: block;
    }
}
```

---

## Imágenes Responsive

```css
img {
    max-width: 100%;
    height: auto;
}

/* Imágenes con aspect ratio */
.img-container {
    position: relative;
    width: 100%;
    padding-bottom: 56.25%; /* 16:9 */
    overflow: hidden;
}

.img-container img {
    position: absolute;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    object-fit: cover;
}
```

---

## Tipografía Responsive

```css
:root {
    --font-size-base: 16px;
    --font-size-h1: 1.75rem;
    --font-size-h2: 1.5rem;
    --font-size-h3: 1.25rem;
}

@media (min-width: 768px) {
    :root {
        --font-size-h1: 2rem;
        --font-size-h2: 1.75rem;
        --font-size-h3: 1.5rem;
    }
}

@media (min-width: 1200px) {
    :root {
        --font-size-h1: 2.5rem;
        --font-size-h2: 2rem;
        --font-size-h3: 1.75rem;
    }
}

h1 { font-size: var(--font-size-h1); }
h2 { font-size: var(--font-size-h2); }
h3 { font-size: var(--font-size-h3); }
```

---

## Testing Responsive

### Viewport Meta Tag (Obligatorio)

```html
<meta name="viewport" content="width=device-width, initial-scale=1.0">
```

### Checklist de Pruebas

- [ ] iPhone SE (375px)
- [ ] iPhone 12/13 (390px)
- [ ] iPad (768px)
- [ ] iPad Pro (1024px)
- [ ] Desktop HD (1366px)
- [ ] Desktop Full HD (1920px)

---

*Última actualización: Enero 2026*
