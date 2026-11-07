namespace Encuestas.Model;

/// <summary>Cuenta de acceso: credenciales y correo electrónico.</summary>
public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;

    /// <summary>Indica si el usuario confirmó su correo mediante el enlace enviado al registrarse.</summary>
    public bool EmailConfirmed { get; set; }

    /// <summary>Correo nuevo pendiente de confirmar; se aplica al abrir el enlace enviado a esa dirección.</summary>
    public string? PendingEmail { get; set; }

    /// <summary>Hash de la contraseña (PBKDF2 de ASP.NET Core Identity).</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Valor que cambia cuando se modifican las credenciales. Se incluye en la cookie de sesión
    /// y se valida periódicamente para invalidar sesiones abiertas tras un cambio de contraseña.
    /// </summary>
    public string SecurityStamp { get; set; } = string.Empty;
}
