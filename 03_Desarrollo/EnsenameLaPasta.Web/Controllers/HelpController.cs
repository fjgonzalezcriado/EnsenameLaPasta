using Markdig;
using Microsoft.AspNetCore.Mvc;

namespace EnsenameLaPasta.Web.Controllers;

/// <summary>
/// Sirve la ayuda in-app (manual, glosario, guía de instalación) renderizando los mismos
/// ficheros Markdown de <c>06_Documentacion/Manuales</c> (copiados a la salida en <c>Help/</c>),
/// para no duplicar contenido. El slug se valida contra una lista blanca (sin path traversal).
/// </summary>
public sealed class HelpController : Controller
{
    private static readonly Dictionary<string, (string File, string Title)> Docs =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["manual"] = ("MANUAL_USUARIO.md", "Manual de usuario"),
            ["glosario"] = ("GLOSARIO.md", "Glosario de términos"),
            ["instalacion"] = ("GUIA_INSTALACION.md", "Guía de instalación"),
        };

    private static readonly MarkdownPipeline Pipeline =
        new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    [HttpGet("/ayuda")]
    public IActionResult Index() => View();

    [HttpGet("/ayuda/{doc}")]
    public async Task<IActionResult> Doc(string doc, CancellationToken cancellationToken)
    {
        if (doc is null || !Docs.TryGetValue(doc, out var entry))
            return NotFound();

        var path = Path.Combine(AppContext.BaseDirectory, "Help", entry.File);
        if (!System.IO.File.Exists(path))
            return View(new HelpDocViewModel(entry.Title, "<p>Documento no disponible.</p>"));

        var markdown = await System.IO.File.ReadAllTextAsync(path, cancellationToken);
        var html = Markdown.ToHtml(markdown, Pipeline);
        return View(new HelpDocViewModel(entry.Title, html));
    }
}

/// <summary>Modelo de la vista de un documento de ayuda ya renderizado a HTML.</summary>
public sealed record HelpDocViewModel(string Title, string Html);
