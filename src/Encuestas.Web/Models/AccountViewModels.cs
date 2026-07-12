using System.ComponentModel.DataAnnotations;

namespace Encuestas.Web.Models;

// La política de contraseñas (mínimo 6, máximo 50) se aplica aquí, en el servidor;
// las validaciones de wwwroot/js son solo una mejora de experiencia de usuario.

/// <summary>Modelo de la página de inicio: agrupa acceso y registro con prefijos distintos
/// para que sus mensajes de validación no colisionen (comparten el campo Email).</summary>
public class HomeViewModel
{
    public LoginViewModel Login { get; set; } = new();
    public RegisterViewModel Register { get; set; } = new();
}

public class LoginViewModel
{
    // Sin longitud mínima: cuentas legadas podrían tener contraseñas más cortas que la política actual.
    [Required, EmailAddress, StringLength(50)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string Password { get; set; } = string.Empty;
}

public class RegisterViewModel
{
    [Required, EmailAddress, StringLength(50)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(50, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    [Required, Compare(nameof(Password))]
    public string RePassword { get; set; } = string.Empty;
}

public class EditProfileViewModel
{
    [Required, StringLength(50)]
    public string Name { get; set; } = string.Empty;
}

public class ChangeUserViewModel
{
    [Required, StringLength(25), RegularExpression("^[0-9a-zA-Z-]+$")]
    public string UserName { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string Password { get; set; } = string.Empty;
}

public class ChangeEmailViewModel
{
    [Required, EmailAddress, StringLength(50)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string Password { get; set; } = string.Empty;
}

public class ChangePasswordViewModel
{
    [Required, StringLength(50, MinimumLength = 6)]
    public string NewPassword { get; set; } = string.Empty;

    [Required, Compare(nameof(NewPassword))]
    public string ReNewPassword { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string Password { get; set; } = string.Empty;
}

public class DeleteAccountViewModel
{
    [Required, StringLength(50)]
    public string Password { get; set; } = string.Empty;
}

public class ForgotPasswordViewModel
{
    [Required, EmailAddress, StringLength(50)]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordViewModel
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required, StringLength(50, MinimumLength = 6)]
    public string NewPassword { get; set; } = string.Empty;

    [Required, Compare(nameof(NewPassword))]
    public string ReNewPassword { get; set; } = string.Empty;
}
