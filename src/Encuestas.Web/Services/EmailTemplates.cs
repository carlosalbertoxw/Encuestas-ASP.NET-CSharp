using System.Text.Encodings.Web;

namespace Encuestas.Web.Services;

/// <summary>Cuerpos HTML de los correos, con el contenido codificado.</summary>
public static class EmailTemplates
{
    public static string Text(string text) => $"<p>{HtmlEncoder.Default.Encode(text)}</p>";

    public static string Link(string text, string url)
    {
        var href = HtmlEncoder.Default.Encode(url);
        return $"<p>{HtmlEncoder.Default.Encode(text)}</p><p><a href=\"{href}\">{href}</a></p>";
    }
}
