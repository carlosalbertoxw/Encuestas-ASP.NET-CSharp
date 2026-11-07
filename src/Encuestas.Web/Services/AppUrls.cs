namespace Encuestas.Web.Services;

/// <summary>
/// Construye las URL absolutas de los correos a partir de la URL pública configurada
/// (<c>App:BaseUrl</c>), nunca de la cabecera Host de la petición: así un atacante no puede
/// hacer que el enlace de restablecimiento de otra persona apunte a su dominio.
/// </summary>
public class AppUrls
{
    private readonly Uri _baseUrl;

    public AppUrls(IConfiguration configuration)
    {
        var value = configuration["App:BaseUrl"];
        if (!Uri.TryCreate(value, UriKind.Absolute, out var baseUrl) || baseUrl.Scheme is not ("http" or "https"))
        {
            throw new InvalidOperationException(
                "Falta App:BaseUrl (variable de entorno App__BaseUrl), la URL pública de la aplicación, p. ej. https://encuestas.example.com.");
        }
        _baseUrl = baseUrl;
    }

    /// <summary>Convierte una ruta relativa (p. ej. la de <c>Url.Action</c>) en una URL absoluta.</summary>
    public string Absolute(string relativePath) => new Uri(_baseUrl, relativePath).ToString();
}
