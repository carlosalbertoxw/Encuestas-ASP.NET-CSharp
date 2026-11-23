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
    [Required(ErrorMessage = ValidationMessages.Required)]
    [EmailAddress(ErrorMessage = ValidationMessages.Email)]
    [StringLength(254, ErrorMessage = ValidationMessages.MaxLength)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(50, ErrorMessage = ValidationMessages.MaxLength)]
    public string Password { get; set; } = string.Empty;
}

public class RegisterViewModel
{
    [Required(ErrorMessage = ValidationMessages.Required)]
    [EmailAddress(ErrorMessage = ValidationMessages.Email)]
    [StringLength(254, ErrorMessage = ValidationMessages.MaxLength)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(50, MinimumLength = 6, ErrorMessage = ValidationMessages.LengthRange)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = ValidationMessages.Required)]
    [Compare(nameof(Password), ErrorMessage = ValidationMessages.PasswordsMatch)]
    public string RePassword { get; set; } = string.Empty;
}

public class EditProfileViewModel
{
    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(50, ErrorMessage = ValidationMessages.MaxLength)]
    public string Name { get; set; } = string.Empty;
}

public class ChangeUserViewModel
{
    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(25, ErrorMessage = ValidationMessages.MaxLength)]
    [RegularExpression("^[0-9a-zA-Z-]+$", ErrorMessage = ValidationMessages.UserName)]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(50, ErrorMessage = ValidationMessages.MaxLength)]
    public string Password { get; set; } = string.Empty;
}

public class ChangeEmailViewModel
{
    [Required(ErrorMessage = ValidationMessages.Required)]
    [EmailAddress(ErrorMessage = ValidationMessages.Email)]
    [StringLength(254, ErrorMessage = ValidationMessages.MaxLength)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(50, ErrorMessage = ValidationMessages.MaxLength)]
    public string Password { get; set; } = string.Empty;
}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(50, MinimumLength = 6, ErrorMessage = ValidationMessages.LengthRange)]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = ValidationMessages.Required)]
    [Compare(nameof(NewPassword), ErrorMessage = ValidationMessages.PasswordsMatch)]
    public string ReNewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(50, ErrorMessage = ValidationMessages.MaxLength)]
    public string Password { get; set; } = string.Empty;
}

public class DeleteAccountViewModel
{
    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(50, ErrorMessage = ValidationMessages.MaxLength)]
    public string Password { get; set; } = string.Empty;
}

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = ValidationMessages.Required)]
    [EmailAddress(ErrorMessage = ValidationMessages.Email)]
    [StringLength(254, ErrorMessage = ValidationMessages.MaxLength)]
    public string Email { get; set; } = string.Empty;
}

public class ResendConfirmationViewModel
{
    [Required(ErrorMessage = ValidationMessages.Required)]
    [EmailAddress(ErrorMessage = ValidationMessages.Email)]
    [StringLength(254, ErrorMessage = ValidationMessages.MaxLength)]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordViewModel
{
    [Required(ErrorMessage = ValidationMessages.Required)]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(50, MinimumLength = 6, ErrorMessage = ValidationMessages.LengthRange)]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = ValidationMessages.Required)]
    [Compare(nameof(NewPassword), ErrorMessage = ValidationMessages.PasswordsMatch)]
    public string ReNewPassword { get; set; } = string.Empty;
}
