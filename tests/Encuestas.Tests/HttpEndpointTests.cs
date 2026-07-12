using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Encuestas.Tests;

/// <summary>
/// Pruebas de humo del pipeline HTTP: health check, autorización, autenticación por cookie y
/// antiforgery. Ejercen la aplicación completa (middleware incluido), no solo los repositorios.
/// </summary>
[Collection("http")]
public partial class HttpEndpointTests
{
    private readonly WebAppFactory _factory;

    public HttpEndpointTests(WebAppFactory factory) => _factory = factory;

    private HttpClient CreateClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Fact]
    public async Task Health_responde_saludable()
    {
        var response = await CreateClient().GetAsync("/health");

        response.EnsureSuccessStatusCode();
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Ruta_protegida_sin_sesion_redirige_al_login()
    {
        var response = await CreateClient().GetAsync("/Poll/Index");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/User/Index", response.Headers.Location!.OriginalString, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Login_con_credenciales_demo_autentica()
    {
        var client = CreateClient();
        var (token, cookie) = await GetAntiforgeryAsync(client);

        var response = await PostFormAsync(client, "/User/Login", cookie, new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Login.Email"] = "demo@encuestas.dev",
            ["Login.Password"] = "demo1234"
        });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Poll", response.Headers.Location!.OriginalString, StringComparison.Ordinal);
        Assert.Contains(response.Headers.GetValues("Set-Cookie"), c => c.Contains("Encuestas.Auth", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Login_con_password_incorrecta_no_autentica()
    {
        var client = CreateClient();
        var (token, cookie) = await GetAntiforgeryAsync(client);

        var response = await PostFormAsync(client, "/User/Login", cookie, new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Login.Email"] = "demo@encuestas.dev",
            ["Login.Password"] = "incorrecta"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain(
            response.Headers.TryGetValues("Set-Cookie", out var cookies) ? cookies : [],
            c => c.Contains("Encuestas.Auth", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Post_sin_token_antiforgery_es_rechazado()
    {
        var response = await CreateClient().PostAsync("/User/Login", new FormUrlEncodedContent(
            new Dictionary<string, string> { ["Login.Email"] = "x@y.z", ["Login.Password"] = "loquesea" }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // --- Utilidades ---

    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")]
    private static partial Regex TokenPattern();

    private static async Task<(string Token, string Cookie)> GetAntiforgeryAsync(HttpClient client)
    {
        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();
        var token = TokenPattern().Match(html).Groups[1].Value;
        var cookie = response.Headers.TryGetValues("Set-Cookie", out var cookies)
            ? string.Join("; ", cookies.Select(c => c.Split(';')[0]))
            : string.Empty;
        return (token, cookie);
    }

    private static async Task<HttpResponseMessage> PostFormAsync(
        HttpClient client, string url, string cookie, Dictionary<string, string> form)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new FormUrlEncodedContent(form)
        };
        request.Headers.Add("Cookie", cookie);
        return await client.SendAsync(request);
    }
}
