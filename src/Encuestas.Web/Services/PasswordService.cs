using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;

namespace Encuestas.Web.Services;

public enum PasswordVerificationOutcome
{
    Failed,
    Success,

    /// <summary>La contraseña es correcta pero el hash usa un formato antiguo o débil y debe regenerarse.</summary>
    SuccessRehashNeeded
}

/// <summary>
/// Hashing de contraseñas con PBKDF2 (ASP.NET Core Identity). Verifica también los hashes
/// SHA1 hexadecimales que generaba la versión anterior de la aplicación, para que las cuentas
/// existentes sigan funcionando; al iniciar sesión se actualizan automáticamente a PBKDF2.
/// </summary>
public partial class PasswordService
{
    private static readonly object HasherContext = new();
    private readonly PasswordHasher<object> _hasher = new();

    [GeneratedRegex("^[0-9a-f]{40}$")]
    private static partial Regex LegacySha1Pattern();

    public string Hash(string password) => _hasher.HashPassword(HasherContext, password);

    public PasswordVerificationOutcome Verify(string storedHash, string password)
    {
        if (LegacySha1Pattern().IsMatch(storedHash))
        {
            return LegacySha1(password) == storedHash
                ? PasswordVerificationOutcome.SuccessRehashNeeded
                : PasswordVerificationOutcome.Failed;
        }

        return _hasher.VerifyHashedPassword(HasherContext, storedHash, password) switch
        {
            PasswordVerificationResult.Success => PasswordVerificationOutcome.Success,
            PasswordVerificationResult.SuccessRehashNeeded => PasswordVerificationOutcome.SuccessRehashNeeded,
            _ => PasswordVerificationOutcome.Failed
        };
    }

    // La versión .NET Framework hasheaba con SHA1 sobre bytes ASCII; se replica igual
    // para que los hashes almacenados coincidan.
    private static string LegacySha1(string password)
    {
        var bytes = SHA1.HashData(Encoding.ASCII.GetBytes(password));
        return Convert.ToHexStringLower(bytes);
    }
}
