using Encuestas.Web.Services;
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
}
