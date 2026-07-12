using System.Globalization;
using Microsoft.AspNetCore.DataProtection;

namespace Encuestas.Web.Services;

/// <summary>
/// Genera y valida tokens firmados y con caducidad (ASP.NET Data Protection) para confirmar el
/// correo y restablecer la contraseña. Son *stateless*: no se guardan en la BD. El token de
/// restablecimiento incluye el sello de seguridad del usuario, así que se invalida en cuanto la
/// contraseña cambia (uso único).
/// </summary>
public class TokenService
{
    private const string ConfirmPrefix = "confirm:";
    private const string ResetPrefix = "reset:";
    private static readonly TimeSpan ConfirmLifetime = TimeSpan.FromHours(24);
    private static readonly TimeSpan ResetLifetime = TimeSpan.FromHours(1);

    private readonly ITimeLimitedDataProtector _protector;

    public TokenService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("Encuestas.AccountTokens").ToTimeLimitedDataProtector();
    }

    public string CreateEmailConfirmationToken(int userId) =>
        _protector.Protect($"{ConfirmPrefix}{userId.ToString(CultureInfo.InvariantCulture)}", ConfirmLifetime);

    public int? ValidateEmailConfirmationToken(string token)
    {
        var payload = TryUnprotect(token);
        if (payload is null || !payload.StartsWith(ConfirmPrefix, StringComparison.Ordinal))
        {
            return null;
        }
        return int.TryParse(payload[ConfirmPrefix.Length..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
            ? id
            : null;
    }

    public string CreatePasswordResetToken(int userId, string securityStamp) =>
        _protector.Protect($"{ResetPrefix}{userId.ToString(CultureInfo.InvariantCulture)}:{securityStamp}", ResetLifetime);

    public (int UserId, string SecurityStamp)? ValidatePasswordResetToken(string token)
    {
        var payload = TryUnprotect(token);
        if (payload is null || !payload.StartsWith(ResetPrefix, StringComparison.Ordinal))
        {
            return null;
        }
        var parts = payload[ResetPrefix.Length..].Split(':', 2);
        return parts.Length == 2 && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)
            ? (id, parts[1])
            : null;
    }

    private string? TryUnprotect(string token)
    {
        try
        {
            return _protector.Unprotect(token);
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            // Token manipulado, mal formado o expirado.
            return null;
        }
    }
}
