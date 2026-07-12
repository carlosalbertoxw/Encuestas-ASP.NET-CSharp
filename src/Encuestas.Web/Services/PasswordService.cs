using Microsoft.AspNetCore.Identity;

namespace Encuestas.Web.Services;

public enum PasswordVerificationOutcome
{
    Failed,
    Success,

    /// <summary>La contraseña es correcta pero el hash usa parámetros antiguos y conviene regenerarlo.</summary>
    SuccessRehashNeeded
}

/// <summary>
/// Hashing y verificación de contraseñas con PBKDF2 (ASP.NET Core Identity). Cuando Identity
/// endurece sus parámetros por defecto, <see cref="Verify"/> señala con
/// <see cref="PasswordVerificationOutcome.SuccessRehashNeeded"/> que el hash almacenado debe
/// regenerarse; el flujo de inicio de sesión lo actualiza de forma transparente.
/// </summary>
public class PasswordService
{
    private static readonly object HasherContext = new();
    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(HasherContext, password);

    public PasswordVerificationOutcome Verify(string storedHash, string password)
    {
        try
        {
            return _hasher.VerifyHashedPassword(HasherContext, storedHash, password) switch
            {
                PasswordVerificationResult.Success => PasswordVerificationOutcome.Success,
                PasswordVerificationResult.SuccessRehashNeeded => PasswordVerificationOutcome.SuccessRehashNeeded,
                _ => PasswordVerificationOutcome.Failed
            };
        }
        catch (FormatException)
        {
            // Hash almacenado corrupto o en un formato desconocido: se trata como credencial inválida.
            return PasswordVerificationOutcome.Failed;
        }
    }
}
