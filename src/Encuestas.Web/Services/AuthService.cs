using System.Globalization;
using System.Security.Claims;
using Encuestas.Data;
using Encuestas.Model;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Encuestas.Web.Services;

public enum LoginResult
{
    Success,
    InvalidCredentials,
    EmailNotConfirmed,
    LockedOut
}

/// <summary>
/// Concentra la lógica de autenticación (verificar credenciales, migrar hashes legados,
/// emitir la cookie con los claims) para mantenerla fuera de los controladores y poder probarla.
/// </summary>
public class AuthService(IUserRepository users, PasswordService passwords, AccountLockout lockout, ILogger<AuthService> logger)
{
    /// <summary>Claim con el sello de seguridad; se compara contra la BD para invalidar sesiones.</summary>
    public const string SecurityStampClaimType = "Encuestas:SecurityStamp";

    public async Task<LoginResult> LoginAsync(HttpContext http, string email, string password)
    {
        if (lockout.IsLocked(email))
        {
            logger.LogWarning("Inicio de sesión bloqueado por exceso de intentos desde {RemoteIp}", http.Connection.RemoteIpAddress);
            return LoginResult.LockedOut;
        }

        var profile = await users.GetProfileByEmailAsync(email);
        var outcome = profile is null
            ? PasswordVerificationOutcome.Failed
            : passwords.Verify(profile.User.PasswordHash, password);

        if (outcome == PasswordVerificationOutcome.Failed)
        {
            lockout.RecordFailure(email);
            // El correo no se registra: es un dato personal y lo escribe quien intenta entrar.
            logger.LogWarning("Intento de inicio de sesión fallido (usuario {UserId}) desde {RemoteIp}",
                profile?.User.Id, http.Connection.RemoteIpAddress);
            return LoginResult.InvalidCredentials;
        }

        // Credenciales correctas: se limpia el contador de intentos fallidos.
        lockout.Reset(email);

        if (!profile!.User.EmailConfirmed)
        {
            return LoginResult.EmailNotConfirmed;
        }

        if (outcome == PasswordVerificationOutcome.SuccessRehashNeeded)
        {
            // El hash usa parámetros antiguos: se regenera de forma transparente. No rota el
            // sello de seguridad para no cerrar otras sesiones válidas del usuario.
            await users.UpdatePasswordAsync(profile.User.Id, passwords.Hash(password));
            logger.LogInformation("Hash de contraseña actualizado para el usuario {UserId}", profile.User.Id);
        }

        await SignInAsync(http, profile);
        logger.LogInformation("Inicio de sesión exitoso del usuario {UserId}", profile.User.Id);
        return LoginResult.Success;
    }

    /// <summary>Emite (o reemite) la cookie de autenticación con los datos actuales del perfil.</summary>
    public async Task SignInAsync(HttpContext http, UserProfile profile)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, profile.User.Id.ToString(CultureInfo.InvariantCulture)),
            new(ClaimTypes.Name, profile.UserName),
            new(ClaimTypes.GivenName, profile.Name),
            new(ClaimTypes.Email, profile.User.Email),
            new(SecurityStampClaimType, profile.User.SecurityStamp)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    }

    /// <summary>Reemite la cookie tras editar perfil, usuario o contraseña.</summary>
    public async Task RefreshClaimsAsync(HttpContext http, int userId)
    {
        if (await users.GetProfileByIdAsync(userId) is { } profile)
        {
            await SignInAsync(http, profile);
        }
    }

    public Task SignOutAsync(HttpContext http) =>
        http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
}
