# Layout Estándar Comillas

> Estructura de diseño web estándar para aplicaciones de Comillas.

---

## Estructura General

```
+------------------------------------------------------------------+
|                         HEADER (60px)                             |
+------------------------------------------------------------------+
|  SIDEBAR   |                                                      |
|  (250px)   |              CONTENIDO PRINCIPAL                     |
|            |                                                      |
|  colapsable|              (100% - 250px)                          |
|  a 60px    |                                                      |
+------------+------------------------------------------------------+
|                         FOOTER (40px)                             |
+------------------------------------------------------------------+
```

---

## Header

### Características
- **Altura fija**: 60px
- **Posición**: `fixed` (sticky en móvil)
- **Fondo**: `var(--comillas-azul)` (#003366)
- **Texto**: Blanco

### Contenido
```
[Logo Comillas] [Nombre Aplicación]                    [Usuario] [v]
```

- **Logo**: 40px de alto, alineado izquierda
- **Nombre App**: Texto blanco, font-size 1.2rem
- **Menú Usuario**: Dropdown con nombre, perfil, cerrar sesión
- **Hamburger**: Solo visible en móvil (<768px)

### HTML Semántico
```html
<header role="banner" class="header">
    <a href="#main-content" class="skip-link">Saltar al contenido</a>
    <div class="header-brand">
        <img src="/images/logo-comillas-blanco.svg" alt="Comillas">
        <span class="app-name">Nombre Aplicación</span>
    </div>
    <nav class="header-nav" aria-label="Usuario">
        <div class="user-menu">...</div>
    </nav>
</header>
```

---

## Sidebar

### Características
- **Ancho**: 250px (expandido) / 60px (colapsado)
- **Posición**: `fixed`, left: 0
- **Altura**: `calc(100vh - 60px - 40px)`
- **Fondo**: `var(--comillas-gris-claro)` (#F5F5F5)
- **Borde derecho**: 1px solid #DDD

### Comportamiento
- **Desktop (>1200px)**: Siempre expandido
- **Tablet (768-1200px)**: Colapsado por defecto, expandible
- **Mobile (<768px)**: Oculto, hamburger menu

### Navegación
```
[Icono] Inicio
[Icono] Módulo 1
  └─ Subopción 1
  └─ Subopción 2
[Icono] Módulo 2
[Icono] Configuración
```

### HTML Semántico
```html
<aside class="sidebar" aria-label="Navegación principal">
    <nav role="navigation">
        <ul class="nav-menu">
            <li class="nav-item">
                <a href="/" class="nav-link active">
                    <span class="nav-icon">🏠</span>
                    <span class="nav-text">Inicio</span>
                </a>
            </li>
            <li class="nav-item has-submenu">
                <button class="nav-link" aria-expanded="false">
                    <span class="nav-icon">📋</span>
                    <span class="nav-text">Módulo 1</span>
                    <span class="nav-arrow">▼</span>
                </button>
                <ul class="nav-submenu">
                    <li><a href="/modulo1/opcion1">Subopción 1</a></li>
                    <li><a href="/modulo1/opcion2">Subopción 2</a></li>
                </ul>
            </li>
        </ul>
    </nav>
    <button class="sidebar-toggle" aria-label="Colapsar menú">
        ◀
    </button>
</aside>
```

---

## Contenido Principal

### Características
- **Margin-left**: 250px (sidebar expandido) / 60px (colapsado)
- **Margin-top**: 60px (header)
- **Margin-bottom**: 40px (footer)
- **Padding**: 24px
- **Background**: `var(--comillas-blanco)`

### Estructura Interna
```
+--------------------------------------------------+
|  BREADCRUMBS                                      |
|  Inicio > Módulo > Página actual                  |
+--------------------------------------------------+
|                                                   |
|  <main id="main-content" role="main">            |
|                                                   |
|    <h1>Título de la Página</h1>                  |
|                                                   |
|    [Contenido dinámico - @RenderBody()]          |
|                                                   |
|  </main>                                          |
|                                                   |
+--------------------------------------------------+
```

### HTML Semántico
```html
<div class="content-wrapper">
    <nav aria-label="Breadcrumb" class="breadcrumb">
        <ol>
            <li><a href="/">Inicio</a></li>
            <li><a href="/modulo">Módulo</a></li>
            <li aria-current="page">Página actual</li>
        </ol>
    </nav>

    <main id="main-content" role="main" tabindex="-1">
        @RenderBody()
    </main>
</div>
```

---

## Footer

### Características
- **Altura fija**: 40px
- **Posición**: `fixed` bottom
- **Fondo**: `var(--comillas-gris-claro)`
- **Texto**: `var(--comillas-gris-medio)`
- **Borde superior**: 1px solid #DDD

### Contenido
```
Universidad Pontificia Comillas | v1.0.0 | Política de Privacidad | Contacto
```

### HTML Semántico
```html
<footer role="contentinfo" class="footer">
    <span>Universidad Pontificia Comillas</span>
    <span class="separator">|</span>
    <span class="version">v1.0.0</span>
    <span class="separator">|</span>
    <a href="/privacidad">Política de Privacidad</a>
    <span class="separator">|</span>
    <a href="/contacto">Contacto</a>
</footer>
```

---

## CSS de Estructura

```css
/* Variables de layout */
:root {
    --header-height: 60px;
    --footer-height: 40px;
    --sidebar-width: 250px;
    --sidebar-collapsed: 60px;
    --content-padding: 24px;
}

/* Layout grid */
body {
    display: grid;
    grid-template-areas:
        "header header"
        "sidebar main"
        "footer footer";
    grid-template-columns: var(--sidebar-width) 1fr;
    grid-template-rows: var(--header-height) 1fr var(--footer-height);
    min-height: 100vh;
}

.header { grid-area: header; }
.sidebar { grid-area: sidebar; }
.content-wrapper { grid-area: main; }
.footer { grid-area: footer; }

/* Sidebar colapsado */
body.sidebar-collapsed {
    grid-template-columns: var(--sidebar-collapsed) 1fr;
}
```

---

## Transiciones

```css
/* Animación de sidebar */
.sidebar {
    transition: width 0.3s ease;
}

.nav-text {
    transition: opacity 0.2s ease;
}

.sidebar-collapsed .nav-text {
    opacity: 0;
    width: 0;
    overflow: hidden;
}
```

---

## Z-Index

| Elemento | Z-Index |
|----------|---------|
| Header | 1000 |
| Sidebar | 900 |
| Modales | 1100 |
| Tooltips | 1200 |
| Notificaciones | 1300 |

---

*Última actualización: Enero 2026*
