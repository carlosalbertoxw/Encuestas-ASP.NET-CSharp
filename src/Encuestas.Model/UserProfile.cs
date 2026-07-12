namespace Encuestas.Model;

/// <summary>Perfil público asociado a una cuenta de usuario.</summary>
public class UserProfile
{
    public User User { get; set; } = new();
    public string UserName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
