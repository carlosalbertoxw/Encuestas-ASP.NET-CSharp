namespace Encuestas.Web.Infrastructure;

/// <summary>
/// Sonda del HEALTHCHECK de Docker (<c>dotnet Encuestas.Web.dll healthcheck</c>). La imagen
/// chiseled no trae curl ni shell, así que la propia aplicación consulta su endpoint de liveness.
/// </summary>
public static class LocalHealthCheck
{
    public static async Task<int> RunAsync()
    {
        var port = Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS")?.Split(';')[0] ?? "8080";
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
        try
        {
            using var response = await client.GetAsync(new Uri($"http://localhost:{port}/health/live"));
            return response.IsSuccessStatusCode ? 0 : 1;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return 1;
        }
    }
}
