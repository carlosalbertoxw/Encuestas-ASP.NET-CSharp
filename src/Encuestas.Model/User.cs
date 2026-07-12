namespace Encuestas.Model;

/// <summary>Cuenta de acceso: credenciales y correo electrónico.</summary>
public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;

    /// <summary>Hash de la contraseña (PBKDF2 de ASP.NET Core Identity, o SHA1 hexadecimal legado).</summary>
    public string PasswordHash { get; set; } = string.Empty;
}
