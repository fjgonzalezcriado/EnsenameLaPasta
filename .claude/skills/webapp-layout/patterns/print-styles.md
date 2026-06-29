# Estilos de Impresion

> Skill: webapp-layout | Version: 3.1.0

Estilos CSS para impresion de paginas web Comillas.

---

## Media Print

```css
@media print {
    /* Ocultar navegacion */
    nav, .sidebar, .header-actions, footer, .no-print { display: none !important; }

    /* Layout a ancho completo */
    .content { width: 100%; margin: 0; padding: 0; }

    /* Colores para impresion */
    body { color: #000; background: #fff; font-size: 12pt; }
    a { color: #000; text-decoration: underline; }
    a[href]::after { content: " (" attr(href) ")"; font-size: 0.8em; }

    /* Control de pagina */
    h1, h2, h3 { page-break-after: avoid; }
    table, figure { page-break-inside: avoid; }
    .page-break { page-break-before: always; }

    /* Header impreso */
    .print-header {
        display: block !important;
        text-align: center;
        border-bottom: 2px solid #003366;
        padding-bottom: 10px;
        margin-bottom: 20px;
    }
    .print-header img { max-height: 50px; }
}
```

## Uso

Agregar clase `no-print` a elementos que no deben imprimirse.
Agregar clase `page-break` donde se necesite salto de pagina.

---

*Pattern v3.1.0*
