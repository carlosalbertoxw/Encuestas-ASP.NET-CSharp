using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Encuestas.Tests;

/// <summary>
/// Flujos completos de la cuenta a través de HTTP: los enlaces se leen de los correos que el
/// despachador de la bandeja de salida entrega al emisor de prueba.
/// </summary>
[Collection("http")]
[Trait("Category", "Integration")]
public partial class AccountFlowTests(WebAppFactory factory)
{
    private const string Password = "Secreta123";

    [Fact]
    public async Task Registro_confirmacion_e_inicio_de_sesion()
    {
        var email = NewEmail();
        var client = CreateClient();

        await RegisterAsync(client, email);
        var link = ExtractLink(await factory.Emails.WaitForAsync(email, "Confirma tu cuenta"));

        // Antes de confirmar, el login se rechaza.
        var beforeConfirm = await LoginAsync(client, email, Password);
        Assert.Equal(HttpStatusCode.OK, beforeConfirm.StatusCode);
        Assert.Contains("Debes confirmar tu correo", await ReadAsync(beforeConfirm), StringComparison.Ordinal);

        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync(link, Ct)).StatusCode);
        AssertRedirectsTo(await LoginAsync(client, email, Password), "/Poll");
    }

    [Fact]
    public async Task Reenvio_de_confirmacion_manda_un_enlace_nuevo_valido()
    {
        var email = NewEmail();
        var client = CreateClient();
        await RegisterAsync(client, email);
        await factory.Emails.WaitForAsync(email, "Confirma tu cuenta");

        var token = await GetTokenAsync(client, "/User/ResendConfirmation");
        var response = await PostAsync(client, "/User/ResendConfirmation", token, new() { ["Email"] = email });
        Assert.Contains("te enviamos un enlace nuevo", await ReadAsync(response), StringComparison.Ordinal);

        var link = ExtractLink(await factory.Emails.WaitForAsync(email, "Confirma tu cuenta", count: 2));
        await client.GetAsync(link, Ct);
        AssertRedirectsTo(await LoginAsync(client, email, Password), "/Poll");
    }

    [Fact]
    public async Task Restablecer_contraseña_cambia_la_contraseña_y_el_enlace_es_de_un_solo_uso()
    {
        var email = await CreateConfirmedAccountAsync();
        var client = CreateClient();

        var token = await GetTokenAsync(client, "/User/ForgotPassword");
        await PostAsync(client, "/User/ForgotPassword", token, new() { ["Email"] = email });
        var link = ExtractLink(await factory.Emails.WaitForAsync(email, "Restablece tu contraseña"));
        var resetToken = Uri.UnescapeDataString(link[(link.IndexOf("token=", StringComparison.Ordinal) + 6)..]);

        Dictionary<string, string> Form() => new()
        {
            ["Token"] = resetToken,
            ["NewPassword"] = "Nueva12345",
            ["ReNewPassword"] = "Nueva12345"
        };
        AssertRedirectsTo(await PostAsync(client, "/User/ResetPassword", await GetTokenAsync(client, link), Form()), "/");
        AssertRedirectsTo(await LoginAsync(CreateClient(), email, "Nueva12345"), "/Poll");

        // Usar el mismo enlace otra vez no funciona: el sello de seguridad ya cambió.
        var reused = await PostAsync(client, "/User/ResetPassword", await GetTokenAsync(client, "/User/ForgotPassword"), Form());
        Assert.Contains("no es válido o expiró", await ReadAsync(reused), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Cambio_de_correo_se_aplica_al_abrir_el_enlace_del_correo_nuevo()
    {
        var email = await CreateConfirmedAccountAsync();
        var newEmail = NewEmail();
        var client = CreateClient();
        AssertRedirectsTo(await LoginAsync(client, email, Password), "/Poll");

        var token = await GetTokenAsync(client, "/Account/ChangeEmail");
        await PostAsync(client, "/Account/ChangeEmail", token, new() { ["Email"] = newEmail, ["Password"] = Password });
        await factory.Emails.WaitForAsync(email, "Solicitud de cambio de correo");
        var link = ExtractLink(await factory.Emails.WaitForAsync(newEmail, "Confirma tu nuevo correo"));

        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync(link, Ct)).StatusCode);
        AssertRedirectsTo(await LoginAsync(CreateClient(), newEmail, Password), "/Poll");
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(CreateClient(), email, Password)).StatusCode);
    }

    [Fact]
    public async Task Encuesta_invalida_vuelve_a_mostrar_el_formulario_con_lo_escrito()
    {
        var client = CreateClient();
        AssertRedirectsTo(await LoginAsync(client, "demo@encuestas.dev", "demo1234"), "/Poll");

        var token = await GetTokenAsync(client, "/Poll/Add");
        var response = await PostAsync(client, "/Poll/Add", token, new()
        {
            ["Title"] = string.Empty,
            ["Description"] = "Texto que no debe perderse",
            ["Position"] = "1"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Texto que no debe perderse", await ReadAsync(response), StringComparison.Ordinal);
    }

    // --- Utilidades ---

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private HttpClient CreateClient() =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

    private static string NewEmail() => $"flujo-{Guid.NewGuid():N}@test.com";

    /// <summary>HTML de la respuesta decodificado (Razor codifica los acentos como entidades).</summary>
    private static async Task<string> ReadAsync(HttpResponseMessage response) =>
        WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync(Ct));

    private async Task<string> CreateConfirmedAccountAsync()
    {
        var email = NewEmail();
        var client = CreateClient();
        await RegisterAsync(client, email);
        await client.GetAsync(ExtractLink(await factory.Emails.WaitForAsync(email, "Confirma tu cuenta")), Ct);
        return email;
    }

    private static async Task RegisterAsync(HttpClient client, string email)
    {
        var token = await GetTokenAsync(client, "/");
        var response = await PostAsync(client, "/User/Register", token, new()
        {
            ["Register.Email"] = email,
            ["Register.Password"] = Password,
            ["Register.RePassword"] = Password
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static async Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password)
    {
        var token = await GetTokenAsync(client, "/");
        return await PostAsync(client, "/User/Login", token, new()
        {
            ["Login.Email"] = email,
            ["Login.Password"] = password
        });
    }

    private static async Task<string> GetTokenAsync(HttpClient client, string url)
    {
        var html = await client.GetStringAsync(url, Ct);
        return TokenPattern().Match(html).Groups[1].Value;
    }

    private static async Task<HttpResponseMessage> PostAsync(
        HttpClient client, string url, string antiforgeryToken, Dictionary<string, string> form)
    {
        form["__RequestVerificationToken"] = antiforgeryToken;
        using var content = new FormUrlEncodedContent(form);
        return await client.PostAsync(url, content, Ct);
    }

    /// <summary>Ruta relativa (con su query) del primer enlace del cuerpo HTML del correo.</summary>
    private static string ExtractLink(string body) =>
        new Uri(WebUtility.HtmlDecode(LinkPattern().Match(body).Groups[1].Value)).PathAndQuery;

    private static void AssertRedirectsTo(HttpResponseMessage response, string path)
    {
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.StartsWith(path, response.Headers.Location!.OriginalString, StringComparison.Ordinal);
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")]
    private static partial Regex TokenPattern();

    [GeneratedRegex("href=\"([^\"]+)\"")]
    private static partial Regex LinkPattern();
}
