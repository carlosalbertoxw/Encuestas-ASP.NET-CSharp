using Encuestas.Model;

namespace Encuestas.Data;

public interface IUserRepository
{
    /// <summary>Crea la cuenta y su perfil (usuario{id}) en una sola transacción.</summary>
    /// <returns><see cref="RepositoryResult.Duplicate"/> si el correo ya existe.</returns>
    Task<RepositoryResult> CreateUserAsync(string email, string passwordHash, string securityStamp);

    Task<User?> GetUserAsync(int id);
    Task<UserProfile?> GetProfileByIdAsync(int id);
    Task<UserProfile?> GetProfileByEmailAsync(string email);
    Task<UserProfile?> GetProfileByUserNameAsync(string userName);

    /// <summary>Sello de seguridad actual de la cuenta, o <c>null</c> si no existe.</summary>
    Task<string?> GetSecurityStampAsync(int id);

    /// <summary>Marca el correo del usuario como confirmado.</summary>
    Task<RepositoryResult> ConfirmEmailAsync(int id);

    Task<RepositoryResult> UpdateNameAsync(int id, string name);

    Task<RepositoryResult> UpdateUserNameAsync(int id, string userName);

    /// <summary>
    /// Guarda el correo nuevo como pendiente. La cuenta sigue usando el correo actual hasta que
    /// se confirma el cambio con <see cref="ConfirmEmailChangeAsync"/>.
    /// </summary>
    Task<RepositoryResult> SetPendingEmailAsync(int id, string email);

    /// <summary>
    /// Aplica el correo pendiente si sigue siendo <paramref name="email"/> y rota el sello de
    /// seguridad para cerrar las demás sesiones.
    /// </summary>
    /// <returns><see cref="RepositoryResult.Duplicate"/> si otra cuenta ya usa ese correo;
    /// <see cref="RepositoryResult.NotFound"/> si el cambio pendiente ya no coincide.</returns>
    Task<RepositoryResult> ConfirmEmailChangeAsync(int id, string email, string newSecurityStamp);

    /// <summary>Actualiza solo el hash (p. ej. al regenerarlo con parámetros nuevos); conserva el sello de seguridad.</summary>
    Task<RepositoryResult> UpdatePasswordAsync(int id, string passwordHash);

    /// <summary>Cambia la contraseña y rota el sello de seguridad para invalidar otras sesiones.</summary>
    Task<RepositoryResult> ChangePasswordAsync(int id, string passwordHash, string newSecurityStamp);

    /// <summary>Elimina la cuenta; el esquema borra en cascada perfil, encuestas, respuestas y sesiones.</summary>
    Task<RepositoryResult> DeleteAccountAsync(int id);
}
