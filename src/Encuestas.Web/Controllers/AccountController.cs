using Encuestas.Data;
using Encuestas.Web.Models;
using Encuestas.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Encuestas.Web.Controllers;

/// <summary>Gestión de la cuenta autenticada: perfil, usuario, correo, contraseña y borrado.</summary>
[Authorize]
public class AccountController(
    IUserRepository users,
    PasswordService passwords,
    AuthService auth,
    TokenService tokens,
    IEmailSender email,
    AppUrls urls,
    SecurityStampCache stampCache,
    ILogger<AccountController> logger) : Controller
{
    [HttpGet]
    public IActionResult EditProfile() =>
        View(new EditProfileViewModel { Name = User.GetGivenName() });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProfile(EditProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        ViewBag.Message = await users.UpdateNameAsync(User.GetUserId(), model.Name) == RepositoryResult.Success
            ? await RefreshClaimsAsync()
            : Messages.UpdateError;
        return View(model);
    }

    [HttpGet]
    public IActionResult ChangeUser() =>
        View(new ChangeUserViewModel { UserName = User.Identity!.Name ?? string.Empty });

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeUser(ChangeUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!await VerifyCurrentPasswordAsync(model.Password))
        {
            ViewBag.Message = Messages.WrongPassword;
            return View(model);
        }

        ViewBag.Message = await users.UpdateUserNameAsync(User.GetUserId(), model.UserName) switch
        {
            RepositoryResult.Success => await RefreshClaimsAsync(),
            RepositoryResult.Duplicate => Messages.UserNameTaken,
            _ => Messages.UpdateError
        };
        return View(model);
    }

    [HttpGet]
    public IActionResult ChangeEmail() =>
        View(new ChangeEmailViewModel { Email = User.GetEmail() });

    /// <summary>
    /// Solicita el cambio de correo: el nuevo queda pendiente y solo se aplica cuando se abre el
    /// enlace enviado a esa dirección. Se avisa además al correo actual.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeEmail(ChangeEmailViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!await VerifyCurrentPasswordAsync(model.Password))
        {
            ViewBag.Message = Messages.WrongPassword;
            return View(model);
        }

        var currentEmail = User.GetEmail();
        if (string.Equals(model.Email, currentEmail, StringComparison.OrdinalIgnoreCase))
        {
            ViewBag.Message = Messages.SameEmail;
            return View(model);
        }

        var userId = User.GetUserId();
        if (await users.GetProfileByEmailAsync(model.Email) is not null)
        {
            // No se revela en pantalla que el correo tiene cuenta: solo lo sabe quien controla ese buzón.
            await email.SendAsync(model.Email, "Intento de usar tu correo",
                EmailTemplates.Text("Alguien intentó cambiar el correo de otra cuenta de Encuestas a esta dirección, pero ya tienes una cuenta con ella. No es necesario que hagas nada."));
        }
        else if (await users.SetPendingEmailAsync(userId, model.Email) == RepositoryResult.Success)
        {
            var link = urls.Absolute(Url.Action("ConfirmEmailChange", "Account", new { token = tokens.CreateEmailChangeToken(userId, model.Email) })!);
            await email.SendAsync(model.Email, "Confirma tu nuevo correo",
                EmailTemplates.Link("Para usar esta dirección en tu cuenta de Encuestas visita:", link));
            await email.SendAsync(currentEmail, "Solicitud de cambio de correo",
                EmailTemplates.Text("Se solicitó cambiar el correo de tu cuenta de Encuestas. Si no fuiste tú, cambia tu contraseña: el cambio no se aplicará mientras nadie abra el enlace enviado al correo nuevo."));
            logger.LogInformation("Cambio de correo solicitado por el usuario {UserId}", userId);
        }

        ViewBag.Message = Messages.EmailChangeRequested;
        return View(new ChangeEmailViewModel { Email = currentEmail });
    }

    /// <summary>
    /// Aplica el correo pendiente. Rota el sello de seguridad, así que todas las sesiones (incluida
    /// la actual) se cierran y hay que entrar de nuevo con el correo nuevo.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ConfirmEmailChange(string? token)
    {
        if (token is null || tokens.ValidateEmailChangeToken(token) is not { } change)
        {
            TempData["Message"] = Messages.EmailChangeInvalid;
            return RedirectToAction("Index", "User");
        }

        var result = await users.ConfirmEmailChangeAsync(change.UserId, change.Email, UserController.NewSecurityStamp());
        if (result == RepositoryResult.Success)
        {
            stampCache.Invalidate(change.UserId);
            await auth.SignOutAsync(HttpContext);
            logger.LogInformation("Correo actualizado para el usuario {UserId}", change.UserId);
        }

        TempData["Message"] = result switch
        {
            RepositoryResult.Success => Messages.EmailChanged,
            RepositoryResult.Duplicate => Messages.EmailInUse,
            _ => Messages.EmailChangeInvalid
        };
        return RedirectToAction("Index", "User");
    }

    [HttpGet]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!await VerifyCurrentPasswordAsync(model.Password))
        {
            ViewBag.Message = Messages.WrongPassword;
            return View(model);
        }

        // Rota el sello de seguridad para cerrar otras sesiones; luego reemite la cookie
        // de la sesión actual con el sello nuevo para no expulsar al propio usuario.
        var userId = User.GetUserId();
        if (await users.ChangePasswordAsync(userId, passwords.Hash(model.NewPassword), UserController.NewSecurityStamp()) == RepositoryResult.Success)
        {
            await auth.RefreshClaimsAsync(HttpContext, userId);
            stampCache.Invalidate(userId);
            logger.LogInformation("Contraseña cambiada por el usuario {UserId}", userId);
            ViewBag.Message = Messages.UpdateOk;
        }
        else
        {
            ViewBag.Message = Messages.UpdateError;
        }
        return View(new ChangePasswordViewModel());
    }

    [HttpGet]
    public IActionResult DeleteAccount() => View(new DeleteAccountViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAccount(DeleteAccountViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!await VerifyCurrentPasswordAsync(model.Password))
        {
            ViewBag.Message = Messages.WrongPassword;
            return View(model);
        }

        var userId = User.GetUserId();
        if (await users.DeleteAccountAsync(userId) == RepositoryResult.Success)
        {
            await auth.SignOutAsync(HttpContext);
            stampCache.Invalidate(userId);
            logger.LogInformation("Cuenta eliminada por el usuario {UserId}", userId);
            TempData["Message"] = Messages.AccountDeleted;
            return RedirectToAction("Index", "User");
        }

        ViewBag.Message = Messages.AccountDeleteError;
        return View(model);
    }

    private async Task<bool> VerifyCurrentPasswordAsync(string password) =>
        await users.GetUserAsync(User.GetUserId()) is { } user
        && passwords.Verify(user.PasswordHash, password) != PasswordVerificationOutcome.Failed;

    private async Task<string> RefreshClaimsAsync()
    {
        await auth.RefreshClaimsAsync(HttpContext, User.GetUserId());
        return Messages.UpdateOk;
    }
}
