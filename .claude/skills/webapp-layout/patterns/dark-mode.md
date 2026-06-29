# Modo Oscuro - Implementacion

> Skill: webapp-layout | Version: 3.1.0

Guia para implementar modo oscuro en aplicaciones web Comillas.

---

## CSS Custom Properties

```css
:root {
    --bg-primary: #ffffff;
    --bg-secondary: #f8f9fa;
    --text-primary: #212529;
    --text-secondary: #6c757d;
    --border-color: #dee2e6;
    --accent: #003366; /* Azul Comillas */
}

[data-theme="dark"] {
    --bg-primary: #1a1a2e;
    --bg-secondary: #16213e;
    --text-primary: #e8e8e8;
    --text-secondary: #a0a0a0;
    --border-color: #2d2d44;
    --accent: #4a90d9;
}

body { background: var(--bg-primary); color: var(--text-primary); }
```

## Toggle Button

```html
<button id="theme-toggle" aria-label="Cambiar tema">
    <span class="icon-sun">&#9728;</span>
    <span class="icon-moon">&#9790;</span>
</button>
```

```javascript
const toggle = document.getElementById('theme-toggle');
toggle.addEventListener('click', () => {
    const current = document.documentElement.getAttribute('data-theme');
    const next = current === 'dark' ? 'light' : 'dark';
    document.documentElement.setAttribute('data-theme', next);
    localStorage.setItem('theme', next);
});
// Al cargar: aplicar tema guardado
const saved = localStorage.getItem('theme') ||
    (matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
document.documentElement.setAttribute('data-theme', saved);
```

---

*Pattern v3.1.0*
