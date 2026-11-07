using Encuestas.Web.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.DataProtection;

namespace Encuestas.Tests;

public class TokenServiceTests
{
    private readonly TokenService _tokens = new(new EphemeralDataProtectionProvider());

    [Fact]
    public void Token_de_confirmacion_ida_y_vuelta()
    {
        var token = _tokens.CreateEmailConfirmationToken(42);

        Assert.Equal(42, _tokens.ValidateEmailConfirmationToken(token));
    }

    [Fact]
    public void Token_de_confirmacion_manipulado_es_invalido()
    {
        Assert.Null(_tokens.ValidateEmailConfirmationToken("no-es-un-token"));
    }

    [Fact]
    public void Token_de_restablecimiento_ida_y_vuelta()
    {
        var token = _tokens.CreatePasswordResetToken(7, "sello-123");

        var result = _tokens.ValidatePasswordResetToken(token);
        Assert.Equal((7, "sello-123"), result);
    }

    [Fact]
    public void Un_token_de_confirmacion_no_sirve_como_restablecimiento()
    {
        var confirmToken = _tokens.CreateEmailConfirmationToken(7);

        Assert.Null(_tokens.ValidatePasswordResetToken(confirmToken));
    }

    [Fact]
    public void Un_token_de_restablecimiento_no_sirve_como_confirmacion()
    {
        var resetToken = _tokens.CreatePasswordResetToken(7, "sello-123");

        Assert.Null(_tokens.ValidateEmailConfirmationToken(resetToken));
    }

    [Fact]
    public void Token_de_restablecimiento_manipulado_es_invalido()
    {
        var token = _tokens.CreatePasswordResetToken(7, "sello-123");

        Assert.Null(_tokens.ValidatePasswordResetToken("no-es-un-token"));
        Assert.Null(_tokens.ValidatePasswordResetToken(token + "x"));
    }

    [Fact]
    public void Tokens_de_otra_llave_de_proteccion_no_se_validan()
    {
        var otraInstancia = new TokenService(new EphemeralDataProtectionProvider());
        var tokenAjeno = otraInstancia.CreateEmailConfirmationToken(42);

        Assert.Null(_tokens.ValidateEmailConfirmationToken(tokenAjeno));
    }

    [Fact]
    public void Token_de_cambio_de_correo_ida_y_vuelta()
    {
        var token = _tokens.CreateEmailChangeToken(7, "nuevo@test.com");

        Assert.Equal((7, "nuevo@test.com"), _tokens.ValidateEmailChangeToken(token));
    }

    [Fact]
    public void Un_token_de_confirmacion_no_sirve_para_cambiar_el_correo()
    {
        var confirmToken = _tokens.CreateEmailConfirmationToken(7);

        Assert.Null(_tokens.ValidateEmailChangeToken(confirmToken));
        Assert.Null(_tokens.ValidateEmailConfirmationToken(_tokens.CreateEmailChangeToken(7, "a@b.c")));
    }
}

public class SupportServicesTests
{
    [Theory]
    [InlineData("demo@encuestas.dev", "d***@encuestas.dev")]
    [InlineData("sin-arroba", "***")]
    [InlineData("", "")]
    public void LogRedaction_enmascara_el_correo(string email, string expected)
    {
        Assert.Equal(expected, LogRedaction.Email(email));
    }

    [Fact]
    public void AppUrls_usa_la_url_configurada_y_no_la_de_la_peticion()
    {
        var urls = new AppUrls(Configure("https://encuestas.example.com"));

        Assert.Equal("https://encuestas.example.com/User/ResetPassword?token=abc",
            urls.Absolute("/User/ResetPassword?token=abc"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("no-es-url")]
    [InlineData("ftp://encuestas.example.com")]
    public void AppUrls_sin_url_valida_falla_al_construirse(string? baseUrl)
    {
        Assert.Throws<InvalidOperationException>(() => new AppUrls(Configure(baseUrl)));
    }

    private static IConfiguration Configure(string? baseUrl) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["App:BaseUrl"] = baseUrl })
            .Build();
}
