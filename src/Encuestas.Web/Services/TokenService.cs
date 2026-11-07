using System.Globalization;
using Microsoft.AspNetCore.DataProtection;

namespace Encuestas.Web.Services;

/// <summary>
/// Genera y valida tokens firmados y con caducidad (ASP.NET Data Protection) para confirmar el
/// correo, confirmar un cambio de correo y restablecer la contraseña. Son *stateless*: no se
/// guardan en la BD. El token de restablecimiento incluye el sello de seguridad del usuario, así
/// que se invalida en cuanto la contraseña cambia (uso único).
/// </summary>
public class TokenService(IDataProtectionProvider provider)
{
    private const string ConfirmPrefix = "confirm:";
    private const string ChangeEmailPrefix = "change-email:";
    private const string ResetPrefix = "reset:";
    private static readonly TimeSpan ConfirmLifetime = TimeSpan.FromHours(24);
    private static readonly TimeSpan ResetLifetime = TimeSpan.FromHours(1);

    private readonly ITimeLimitedDataProtector _protector =
        provider.CreateProtector("Encuestas.AccountTokens").ToTimeLimitedDataProtector();

    public string CreateEmailConfirmationToken(int userId) =>
        _protector.Protect(ConfirmPrefix + Format(userId), ConfirmLifetime);

    public int? ValidateEmailConfirmationToken(string token) =>
        TryUnprotect(token, ConfirmPrefix) is { } payload && TryParseId(payload, out var id) ? id : null;

    /// <summary>Token para confirmar el cambio a <paramref name="newEmail"/>; se envía a esa dirección.</summary>
    public string CreateEmailChangeToken(int userId, string newEmail) =>
        _protector.Protect($"{ChangeEmailPrefix}{Format(userId)}:{newEmail}", ConfirmLifetime);

    public (int UserId, string Email)? ValidateEmailChangeToken(string token) =>
        SplitIdAndValue(TryUnprotect(token, ChangeEmailPrefix));

    public string CreatePasswordResetToken(int userId, string securityStamp) =>
        _protector.Protect($"{ResetPrefix}{Format(userId)}:{securityStamp}", ResetLifetime);

    public (int UserId, string SecurityStamp)? ValidatePasswordResetToken(string token) =>
        SplitIdAndValue(TryUnprotect(token, ResetPrefix));

    private static string Format(int userId) => userId.ToString(CultureInfo.InvariantCulture);

    private static bool TryParseId(string value, out int id) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out id);

    private static (int, string)? SplitIdAndValue(string? payload)
    {
        var parts = payload?.Split(':', 2);
        return parts is { Length: 2 } && TryParseId(parts[0], out var id) ? (id, parts[1]) : null;
    }

    /// <summary>Devuelve el contenido sin el prefijo, o <c>null</c> si el token es inválido, expiró o es de otro tipo.</summary>
    private string? TryUnprotect(string token, string prefix)
    {
        try
        {
            var payload = _protector.Unprotect(token);
            return payload.StartsWith(prefix, StringComparison.Ordinal) ? payload[prefix.Length..] : null;
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            // Token manipulado, mal formado o expirado.
            return null;
        }
    }
}
