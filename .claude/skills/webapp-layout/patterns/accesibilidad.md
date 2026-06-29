# Accesibilidad Web - WCAG 2.1 AA

> Guía de accesibilidad para aplicaciones web de Comillas.
> Cumplimiento mínimo: WCAG 2.1 Nivel AA.

---

## Principios POUR

| Principio | Significado | Ejemplos |
|-----------|-------------|----------|
| **Perceptible** | La información debe ser presentable | Alt en imágenes, contraste de colores |
| **Operable** | La interfaz debe ser navegable | Teclado, tiempo suficiente |
| **Comprensible** | El contenido debe ser entendible | Lenguaje claro, errores explicados |
| **Robusto** | Compatible con tecnologías asistivas | HTML válido, ARIA |

---

## 1. Contraste de Colores

### Requisitos WCAG AA

| Tipo | Ratio mínimo |
|------|--------------|
| Texto normal (<18px) | 4.5:1 |
| Texto grande (>=18px bold o >=24px) | 3:1 |
| Componentes UI y gráficos | 3:1 |

### Combinaciones Aprobadas Comillas

```css
/* Texto sobre fondo blanco */
.text-primary { color: #003366; }  /* 12.6:1 OK */
.text-dark { color: #333333; }     /* 12.6:1 OK */
.text-muted { color: #666666; }    /* 5.7:1 OK */

/* Texto sobre fondo azul Comillas */
.text-on-blue { color: #FFFFFF; }  /* 12.6:1 OK */

/* Evitar */
.text-light-blue { color: #0066CC; } /* 4.1:1 NO para texto pequeño */
```

### Herramientas de Verificación

- [WebAIM Contrast Checker](https://webaim.org/resources/contrastchecker/)
- [Colour Contrast Analyser](https://www.tpgi.com/color-contrast-checker/)
- DevTools > Lighthouse > Accessibility

---

## 2. Navegación por Teclado

### Orden de Tabulación

```html
<!-- Orden lógico de lectura -->
<header>...</header>
<nav>...</nav>
<main>
    <h1>Título</h1>
    <form>
        <input tabindex="0"> <!-- Orden natural -->
        <input tabindex="0">
        <button tabindex="0">Enviar</button>
    </form>
</main>
<footer>...</footer>

<!-- NUNCA usar tabindex > 0 -->
<input tabindex="5"> <!-- MAL -->
```

### Skip Link (Obligatorio)

```html
<body>
    <a href="#main-content" class="skip-link">
        Saltar al contenido principal
    </a>
    <header>...</header>
    <nav>...</nav>
    <main id="main-content" tabindex="-1">
        ...
    </main>
</body>
```

```css
.skip-link {
    position: absolute;
    top: -40px;
    left: 0;
    background: var(--comillas-azul);
    color: white;
    padding: 8px 16px;
    z-index: 9999;
    transition: top 0.3s;
}

.skip-link:focus {
    top: 0;
}
```

### Estados de Focus

```css
/* Focus visible obligatorio */
:focus {
    outline: 2px solid var(--comillas-azul-claro);
    outline-offset: 2px;
}

/* No eliminar outline sin alternativa */
:focus:not(:focus-visible) {
    outline: none;
}

:focus-visible {
    outline: 2px solid var(--comillas-azul-claro);
    outline-offset: 2px;
}

/* Botones */
.btn:focus {
    box-shadow: 0 0 0 3px rgba(0, 102, 204, 0.4);
}
```

---

## 3. ARIA Landmarks

### Roles Principales

```html
<header role="banner">
    <!-- Logo, título, menú usuario -->
</header>

<nav role="navigation" aria-label="Menú principal">
    <!-- Navegación principal -->
</nav>

<main role="main" id="main-content">
    <!-- Contenido principal -->
</main>

<aside role="complementary" aria-label="Información adicional">
    <!-- Contenido secundario -->
</aside>

<footer role="contentinfo">
    <!-- Información de pie de página -->
</footer>
```

### Múltiples Navegaciones

```html
<nav aria-label="Navegación principal">...</nav>
<nav aria-label="Breadcrumbs">...</nav>
<nav aria-label="Paginación">...</nav>
```

---

## 4. Formularios Accesibles

### Labels

```html
<!-- Siempre asociar label con input -->
<label for="email">Correo electrónico</label>
<input type="email" id="email" name="email" required>

<!-- O label envolvente -->
<label>
    Correo electrónico
    <input type="email" name="email" required>
</label>
```

### Campos Requeridos

```html
<label for="nombre">
    Nombre <span aria-hidden="true">*</span>
    <span class="sr-only">(requerido)</span>
</label>
<input type="text" id="nombre" name="nombre" required
       aria-required="true">
```

### Mensajes de Error

```html
<div class="form-group">
    <label for="email">Email</label>
    <input type="email" id="email"
           aria-describedby="email-error"
           aria-invalid="true"
           class="is-invalid">
    <div id="email-error" class="error-message" role="alert">
        Por favor, introduce un email válido.
    </div>
</div>
```

### Grupos de Campos

```html
<fieldset>
    <legend>Información de contacto</legend>

    <label for="telefono">Teléfono</label>
    <input type="tel" id="telefono">

    <label for="direccion">Dirección</label>
    <input type="text" id="direccion">
</fieldset>
```

---

## 5. Imágenes y Multimedia

### Imágenes Informativas

```html
<!-- Con información relevante -->
<img src="grafico-ventas.png"
     alt="Gráfico de ventas: Q1 1M€, Q2 1.5M€, Q3 2M€, Q4 2.2M€">

<!-- Decorativas -->
<img src="decoracion.png" alt="" role="presentation">

<!-- Logos -->
<img src="logo-comillas.svg" alt="Universidad Pontificia Comillas">
```

### Iconos

```html
<!-- Icono con texto visible -->
<button>
    <span class="icon" aria-hidden="true">🔍</span>
    Buscar
</button>

<!-- Solo icono -->
<button aria-label="Buscar">
    <span class="icon" aria-hidden="true">🔍</span>
</button>
```

---

## 6. Tablas de Datos

```html
<table>
    <caption>Listado de estudiantes matriculados</caption>
    <thead>
        <tr>
            <th scope="col">Nombre</th>
            <th scope="col">Email</th>
            <th scope="col">Curso</th>
            <th scope="col">Acciones</th>
        </tr>
    </thead>
    <tbody>
        <tr>
            <td>Juan García</td>
            <td>jgarcia@comillas.edu</td>
            <td>3º ADE</td>
            <td>
                <button aria-label="Editar Juan García">Editar</button>
            </td>
        </tr>
    </tbody>
</table>
```

---

## 7. Componentes Interactivos

### Menús Desplegables

```html
<div class="dropdown">
    <button id="menu-btn"
            aria-haspopup="true"
            aria-expanded="false"
            aria-controls="menu-options">
        Opciones
    </button>
    <ul id="menu-options"
        role="menu"
        aria-labelledby="menu-btn"
        hidden>
        <li role="menuitem"><a href="#">Opción 1</a></li>
        <li role="menuitem"><a href="#">Opción 2</a></li>
    </ul>
</div>
```

### Acordeones

```html
<div class="accordion">
    <h3>
        <button id="acc1-btn"
                aria-expanded="false"
                aria-controls="acc1-panel">
            Sección 1
        </button>
    </h3>
    <div id="acc1-panel"
         role="region"
         aria-labelledby="acc1-btn"
         hidden>
        Contenido de la sección 1
    </div>
</div>
```

### Modales

```html
<div class="modal"
     role="dialog"
     aria-modal="true"
     aria-labelledby="modal-title"
     aria-describedby="modal-desc">
    <h2 id="modal-title">Confirmar acción</h2>
    <p id="modal-desc">¿Estás seguro de continuar?</p>
    <button>Confirmar</button>
    <button>Cancelar</button>
</div>
```

---

## 8. CSS Utilidades

```css
/* Ocultar visualmente pero accesible */
.sr-only {
    position: absolute;
    width: 1px;
    height: 1px;
    padding: 0;
    margin: -1px;
    overflow: hidden;
    clip: rect(0, 0, 0, 0);
    white-space: nowrap;
    border: 0;
}

/* Mostrar solo en focus */
.sr-only-focusable:focus {
    position: static;
    width: auto;
    height: auto;
    overflow: visible;
    clip: auto;
    white-space: normal;
}

/* Reducir movimiento */
@media (prefers-reduced-motion: reduce) {
    *,
    *::before,
    *::after {
        animation-duration: 0.01ms !important;
        animation-iteration-count: 1 !important;
        transition-duration: 0.01ms !important;
        scroll-behavior: auto !important;
    }
}
```

---

## Checklist de Verificación

### Estructura
- [ ] HTML semántico (header, nav, main, footer)
- [ ] Roles ARIA en landmarks
- [ ] Skip link al contenido principal
- [ ] Heading hierarchy (h1 > h2 > h3)

### Visual
- [ ] Contraste 4.5:1 para texto
- [ ] Contraste 3:1 para UI
- [ ] No depender solo del color
- [ ] Focus visible en todos los elementos

### Formularios
- [ ] Labels asociados a inputs
- [ ] Campos requeridos identificados
- [ ] Errores anunciados (role="alert")
- [ ] Instrucciones claras

### Interacción
- [ ] Todo operable por teclado
- [ ] Orden de tabulación lógico
- [ ] aria-expanded en menús
- [ ] Modales con focus trap

### Multimedia
- [ ] Alt text en imágenes
- [ ] Iconos decorativos ocultos
- [ ] Videos con subtítulos

---

## Herramientas de Testing

1. **axe DevTools** - Extensión navegador
2. **WAVE** - Evaluador web
3. **Lighthouse** - Auditoría Chrome
4. **NVDA/VoiceOver** - Screen readers
5. **Navegación solo teclado** - Tab, Enter, Espacio, Flechas

---

*Cumplimiento: WCAG 2.1 Nivel AA*
*Última actualización: Enero 2026*
