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
public class AuthService
{
    /// <summary>Claim con el sello de seguridad; se compara contra la BD para invalidar sesiones.</summary>
    public const string SecurityStampClaimType = "Encuestas:SecurityStamp";

    private readonly IUserRepository _users;
    private readonly PasswordService _passwords;
    private readonly AccountLockout _lockout;
    private readonly ILogger<AuthService> _logger;

    public AuthService(IUserRepository users, PasswordService passwords, AccountLockout lockout, ILogger<AuthService> logger)
    {
        _users = users;
        _passwords = passwords;
        _lockout = lockout;
        _logger = logger;
    }

    public async Task<LoginResult> LoginAsync(HttpContext http, string email, string password)
    {
        if (_lockout.IsLocked(email))
        {
            _logger.LogWarning("Inicio de sesión bloqueado por exceso de intentos para {Email}", email);
            return LoginResult.LockedOut;
        }

        var profile = await _users.GetProfileByEmailAsync(email);
        var outcome = profile is null
            ? PasswordVerificationOutcome.Failed
            : _passwords.Verify(profile.User.PasswordHash, password);

        if (outcome == PasswordVerificationOutcome.Failed)
        {
            _lockout.RecordFailure(email);
            _logger.LogWarning("Intento de inicio de sesión fallido para {Email} desde {RemoteIp}",
                email, http.Connection.RemoteIpAddress);
            return LoginResult.InvalidCredentials;
        }

        // Credenciales correctas: se limpia el contador de intentos fallidos.
        _lockout.Reset(email);

        if (!profile!.User.EmailConfirmed)
        {
            return LoginResult.EmailNotConfirmed;
        }

        if (outcome == PasswordVerificationOutcome.SuccessRehashNeeded)
        {
            // El hash usa parámetros antiguos: se regenera de forma transparente. No rota el
            // sello de seguridad para no cerrar otras sesiones válidas del usuario.
            await _users.UpdatePasswordAsync(profile.User.Id, _passwords.Hash(password));
            _logger.LogInformation("Hash de contraseña actualizado para el usuario {UserId}", profile.User.Id);
        }

        await SignInAsync(http, profile);
        _logger.LogInformation("Inicio de sesión exitoso del usuario {UserId}", profile.User.Id);
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

    /// <summary>Reemite la cookie tras editar perfil, usuario o correo.</summary>
    public async Task RefreshClaimsAsync(HttpContext http, int userId)
    {
        var user = await _users.GetUserAsync(userId);
        if (user is null)
        {
            return;
        }
        var profile = await _users.GetProfileByEmailAsync(user.Email);
        if (profile is not null)
        {
            await SignInAsync(http, profile);
        }
    }

    public Task SignOutAsync(HttpContext http) =>
        http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
}
