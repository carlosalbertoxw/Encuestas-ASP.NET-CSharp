using Encuestas.Model;

namespace Encuestas.Data;

public interface IUserRepository
{
    /// <summary>Crea la cuenta y su perfil (usuario{id}) en una sola transacción. Falso si el correo ya existe.</summary>
    Task<bool> CreateUserAsync(string email, string passwordHash);

    Task<User?> GetUserAsync(int id);
    Task<UserProfile?> GetProfileByEmailAsync(string email);
    Task<UserProfile?> GetProfileByUserNameAsync(string userName);

    Task<bool> UpdateNameAsync(int id, string name);

    /// <summary>Falso también si el nombre de usuario ya está tomado.</summary>
    Task<bool> UpdateUserNameAsync(int id, string userName);

    /// <summary>Falso también si el correo ya está registrado.</summary>
    Task<bool> UpdateEmailAsync(int id, string email);

    Task<bool> UpdatePasswordAsync(int id, string passwordHash);

    /// <summary>Elimina la cuenta; el esquema borra en cascada perfil, encuestas y respuestas.</summary>
    Task<bool> DeleteAccountAsync(int id);
}
