using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Encuestas.Web.Services;

public enum PasswordVerificationOutcome
{
    Failed,
    Success,

    /// <summary>La contraseña es correcta pero el hash usa parámetros antiguos y conviene regenerarlo.</summary>
    SuccessRehashNeeded
}

/// <summary>
/// Hashing y verificación de contraseñas con PBKDF2-HMAC-SHA512 (ASP.NET Core Identity). Si el
/// hash almacenado usa menos iteraciones que las actuales, <see cref="Verify"/> devuelve
/// <see cref="PasswordVerificationOutcome.SuccessRehashNeeded"/> y el inicio de sesión lo regenera.
/// </summary>
public class PasswordService
{
    /// <summary>Iteraciones recomendadas por OWASP para PBKDF2-HMAC-SHA512.</summary>
    public const int IterationCount = 210_000;

    private static readonly object HasherContext = new();
    private readonly PasswordHasher<object> _hasher =
        new(Options.Create(new PasswordHasherOptions { IterationCount = IterationCount }));

    // Hash de una contraseña aleatoria: verificarlo cuesta lo mismo que una cuenta real.
    private readonly Lazy<string> _dummyHash;

    public PasswordService() =>
        _dummyHash = new Lazy<string>(() => Hash(Guid.NewGuid().ToString()));

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

    /// <summary>
    /// Hace el mismo trabajo que <see cref="Verify"/> cuando la cuenta no existe, para que el
    /// tiempo de respuesta no revele qué correos están registrados.
    /// </summary>
    public void VerifyAgainstDummy(string password) => Verify(_dummyHash.Value, password);
}
