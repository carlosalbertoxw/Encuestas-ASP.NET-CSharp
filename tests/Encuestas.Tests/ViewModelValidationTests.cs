using System.ComponentModel.DataAnnotations;
using Encuestas.Web.Models;

namespace Encuestas.Tests;

/// <summary>
/// Verifica que la política de validación (incluida la longitud mínima de contraseña)
/// se aplica en el servidor mediante DataAnnotations, no solo en el JavaScript del cliente.
/// </summary>
public class ViewModelValidationTests
{
    private static bool IsValid(object model)
    {
        var context = new ValidationContext(model);
        return Validator.TryValidateObject(model, context, validationResults: null, validateAllProperties: true);
    }

    [Fact]
    public void Registro_valido_pasa()
    {
        Assert.True(IsValid(new RegisterViewModel { Email = "a@b.co", Password = "secreta1", RePassword = "secreta1" }));
    }

    [Theory]
    [InlineData("12345")] // menor al mínimo de 6
    [InlineData("")]
    public void Registro_rechaza_contraseñas_cortas(string password)
    {
        Assert.False(IsValid(new RegisterViewModel { Email = "a@b.co", Password = password, RePassword = password }));
    }

    [Fact]
    public void Registro_rechaza_contraseñas_que_no_coinciden()
    {
        Assert.False(IsValid(new RegisterViewModel { Email = "a@b.co", Password = "secreta1", RePassword = "secreta2" }));
    }

    [Fact]
    public void Registro_rechaza_correo_invalido()
    {
        Assert.False(IsValid(new RegisterViewModel { Email = "no-es-correo", Password = "secreta1", RePassword = "secreta1" }));
    }

    [Fact]
    public void CambioDePassword_exige_minimo_seis_caracteres()
    {
        Assert.False(IsValid(new ChangePasswordViewModel { NewPassword = "corta", ReNewPassword = "corta", Password = "actual123" }));
    }

    [Theory]
    [InlineData("usuario-01", true)]
    [InlineData("usuario con espacios", false)]
    [InlineData("usuario@!", false)]
    public void CambioDeUsuario_valida_caracteres_permitidos(string userName, bool expected)
    {
        Assert.Equal(expected, IsValid(new ChangeUserViewModel { UserName = userName, Password = "secreta1" }));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(999999, true)]
    [InlineData(1000000, false)]
    public void Encuesta_valida_rango_de_posicion(int position, bool expected)
    {
        Assert.Equal(expected, IsValid(new PollFormViewModel { Title = "t", Description = "d", Position = position }));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(5, true)]
    [InlineData(6, false)]
    public void Respuesta_valida_rango_de_estrellas(int stars, bool expected)
    {
        Assert.Equal(expected, IsValid(new AnswerFormViewModel { Stars = stars }));
    }

    [Theory]
    [InlineData(250, 500, true)]
    [InlineData(251, 500, false)]
    [InlineData(250, 501, false)]
    public void Encuesta_valida_longitud_de_titulo_y_descripcion(int titleLength, int descriptionLength, bool expected)
    {
        var model = new PollFormViewModel
        {
            Title = new string('t', titleLength),
            Description = new string('d', descriptionLength),
            Position = 1
        };
        Assert.Equal(expected, IsValid(model));
    }

    [Fact]
    public void Respuesta_acepta_comentario_nulo_y_rechaza_mas_de_mil_caracteres()
    {
        Assert.True(IsValid(new AnswerFormViewModel { Stars = 3, Comment = null }));
        Assert.False(IsValid(new AnswerFormViewModel { Stars = 3, Comment = new string('c', 1001) }));
    }

    [Fact]
    public void Restablecimiento_rechaza_contraseñas_que_no_coinciden_y_token_vacio()
    {
        Assert.False(IsValid(new ResetPasswordViewModel { Token = "tok", NewPassword = "secreta1", ReNewPassword = "secreta2" }));
        Assert.False(IsValid(new ResetPasswordViewModel { Token = "", NewPassword = "secreta1", ReNewPassword = "secreta1" }));
    }

    [Fact]
    public void CambioDeCorreo_rechaza_correo_invalido()
    {
        Assert.False(IsValid(new ChangeEmailViewModel { Email = "no-es-correo", Password = "secreta1" }));
    }

    [Fact]
    public void Login_exige_correo_valido_y_contraseña_presente()
    {
        Assert.False(IsValid(new LoginViewModel { Email = "x", Password = "secreta1" }));
        Assert.False(IsValid(new LoginViewModel { Email = "a@b.co", Password = "" }));
    }
}
