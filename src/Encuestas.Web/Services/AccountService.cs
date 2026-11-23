using Encuestas.Data;
using Encuestas.Model;
using Encuestas.Web.Infrastructure;

namespace Encuestas.Web.Services;

public enum ResetPasswordResult
{
    Success,

    /// <summary>Token manipulado, caducado o ya usado (el sello de seguridad cambió).</summary>
    InvalidToken,
    Error
}

/// <summary>
/// Flujos de la cuenta que combinan tokens, correos y el sello de seguridad: registro y
/// confirmación, recuperación de contraseña, cambio de correo y de contraseña, y borrado.
/// Los controladores solo validan la entrada, llaman aquí y eligen el mensaje; lo relativo a la
/// cookie (emitirla o cerrarla) sigue en <see cref="AuthService"/>.
/// </summary>
public class AccountService(
    IUserRepository users,
    PasswordService passwords,
    TokenService tokens,
    EmailOutbox outbox,
    AppUrls urls,
    SecurityStampCache stampCache,
    ILogger<AccountService> logger)
{
    public static string NewSecurityStamp() => Guid.NewGuid().ToString();

    /// <summary>Crea la cuenta y envía el enlace de confirmación. Si el correo ya existe no hace nada.</summary>
    public async Task RegisterAsync(string email, string password)
    {
        var result = await users.CreateUserAsync(email, passwords.Hash(password), NewSecurityStamp());
        if (result == RepositoryResult.Success && await users.GetProfileByEmailAsync(email) is { } profile)
        {
            await SendConfirmationAsync(profile);
            logger.LogInformation("Cuenta nueva registrada: usuario {UserId}", profile.User.Id);
        }
    }

    /// <summary>Reenvía el enlace de confirmación solo si la cuenta existe y sigue sin confirmar.</summary>
    public async Task ResendConfirmationAsync(string email)
    {
        if (await users.GetProfileByEmailAsync(email) is { User.EmailConfirmed: false } profile)
        {
            await SendConfirmationAsync(profile);
            logger.LogInformation("Enlace de confirmación reenviado al usuario {UserId}", profile.User.Id);
        }
    }

    public async Task<bool> ConfirmEmailAsync(string token)
    {
        if (tokens.ValidateEmailConfirmationToken(token) is not { } userId)
        {
            return false;
        }
        await users.ConfirmEmailAsync(userId);
        logger.LogInformation("Correo confirmado para el usuario {UserId}", userId);
        return true;
    }

    public async Task RequestPasswordResetAsync(string email)
    {
        if (await users.GetProfileByEmailAsync(email) is { } profile)
        {
            var token = tokens.CreatePasswordResetToken(profile.User.Id, profile.User.SecurityStamp);
            await outbox.EnqueueAsync(email, "Restablece tu contraseña",
                EmailTemplates.Link("Para restablecer tu contraseña visita:", Link("/User/ResetPassword", token)));
            logger.LogInformation("Enlace de restablecimiento generado para el usuario {UserId}", profile.User.Id);
        }
    }

    public bool IsPasswordResetTokenValid(string token) => tokens.ValidatePasswordResetToken(token) is not null;

    public async Task<ResetPasswordResult> ResetPasswordAsync(string token, string newPassword)
    {
        // El token incluye el sello de seguridad: si no coincide con el actual, ya se usó o caducó.
        if (tokens.ValidatePasswordResetToken(token) is not { } valid
            || await users.GetSecurityStampAsync(valid.UserId) != valid.SecurityStamp)
        {
            return ResetPasswordResult.InvalidToken;
        }

        if (await users.ChangePasswordAsync(valid.UserId, passwords.Hash(newPassword), NewSecurityStamp()) != RepositoryResult.Success)
        {
            return ResetPasswordResult.Error;
        }
        stampCache.Invalidate(valid.UserId);
        logger.LogInformation("Contraseña restablecida para el usuario {UserId}", valid.UserId);
        return ResetPasswordResult.Success;
    }

    public async Task<bool> VerifyPasswordAsync(int userId, string password) =>
        await users.GetUserAsync(userId) is { } user
        && passwords.Verify(user.PasswordHash, password) != PasswordVerificationOutcome.Failed;

    /// <summary>
    /// Deja <paramref name="newEmail"/> pendiente y envía el enlace a esa dirección, avisando al
    /// correo actual. Si otra cuenta ya lo usa, solo se avisa a ese buzón: la respuesta en pantalla
    /// es la misma, para no revelar qué correos tienen cuenta.
    /// </summary>
    public async Task RequestEmailChangeAsync(int userId, string currentEmail, string newEmail)
    {
        if (await users.GetProfileByEmailAsync(newEmail) is not null)
        {
            await outbox.EnqueueAsync(newEmail, "Intento de usar tu correo",
                EmailTemplates.Text("Alguien intentó cambiar el correo de otra cuenta de Encuestas a esta dirección, pero ya tienes una cuenta con ella. No es necesario que hagas nada."));
            return;
        }

        if (await users.SetPendingEmailAsync(userId, newEmail) == RepositoryResult.Success)
        {
            await outbox.EnqueueAsync(newEmail, "Confirma tu nuevo correo",
                EmailTemplates.Link("Para usar esta dirección en tu cuenta de Encuestas visita:",
                    Link("/Account/ConfirmEmailChange", tokens.CreateEmailChangeToken(userId, newEmail))));
            await outbox.EnqueueAsync(currentEmail, "Solicitud de cambio de correo",
                EmailTemplates.Text("Se solicitó cambiar el correo de tu cuenta de Encuestas. Si no fuiste tú, cambia tu contraseña: el cambio no se aplicará mientras nadie abra el enlace enviado al correo nuevo."));
            logger.LogInformation("Cambio de correo solicitado por el usuario {UserId}", userId);
        }
    }

    /// <summary>
    /// Aplica el correo pendiente y rota el sello de seguridad, lo que cierra todas las sesiones.
    /// </summary>
    /// <returns><see cref="RepositoryResult.NotFound"/> si el token no es válido o el cambio ya no está pendiente.</returns>
    public async Task<RepositoryResult> ConfirmEmailChangeAsync(string token)
    {
        if (tokens.ValidateEmailChangeToken(token) is not { } change)
        {
            return RepositoryResult.NotFound;
        }

        var result = await users.ConfirmEmailChangeAsync(change.UserId, change.Email, NewSecurityStamp());
        if (result == RepositoryResult.Success)
        {
            stampCache.Invalidate(change.UserId);
            logger.LogInformation("Correo actualizado para el usuario {UserId}", change.UserId);
        }
        return result;
    }

    /// <summary>Cambia la contraseña y rota el sello de seguridad para cerrar las demás sesiones.</summary>
    public async Task<bool> ChangePasswordAsync(int userId, string newPassword)
    {
        if (await users.ChangePasswordAsync(userId, passwords.Hash(newPassword), NewSecurityStamp()) != RepositoryResult.Success)
        {
            return false;
        }
        stampCache.Invalidate(userId);
        logger.LogInformation("Contraseña cambiada por el usuario {UserId}", userId);
        return true;
    }

    public async Task<bool> DeleteAccountAsync(int userId)
    {
        if (await users.DeleteAccountAsync(userId) != RepositoryResult.Success)
        {
            return false;
        }
        stampCache.Invalidate(userId);
        logger.LogInformation("Cuenta eliminada por el usuario {UserId}", userId);
        return true;
    }

    private Task SendConfirmationAsync(UserProfile profile) =>
        outbox.EnqueueAsync(profile.User.Email, "Confirma tu cuenta",
            EmailTemplates.Link("Para confirmar tu cuenta visita:",
                Link("/User/ConfirmEmail", tokens.CreateEmailConfirmationToken(profile.User.Id))));

    private string Link(string path, string token) => urls.Absolute($"{path}?token={Uri.EscapeDataString(token)}");
}
