using Encuestas.Data;
using Encuestas.Web.Infrastructure;
using Encuestas.Web.Models;
using Encuestas.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Encuestas.Web.Controllers;

/// <summary>Gestión de la cuenta autenticada: perfil, usuario, correo, contraseña y borrado.</summary>
[Authorize]
public class AccountController(IUserRepository users, AuthService auth, AccountService accounts) : Controller
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

        if (!await accounts.VerifyPasswordAsync(User.GetUserId(), model.Password))
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
    /// enlace enviado a esa dirección. Limitado por usuario, porque cada solicitud envía correos.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RateLimit(RateLimitPolicy.EmailChange)]
    public async Task<IActionResult> ChangeEmail(ChangeEmailViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!await accounts.VerifyPasswordAsync(User.GetUserId(), model.Password))
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

        await accounts.RequestEmailChangeAsync(User.GetUserId(), currentEmail, model.Email);
        ViewBag.Message = Messages.EmailChangeRequested;
        return View(new ChangeEmailViewModel { Email = currentEmail });
    }

    /// <summary>
    /// Aplica el correo pendiente. Rota el sello de seguridad, así que todas las sesiones (incluida
    /// la actual) se cierran y hay que entrar de nuevo con el correo nuevo.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [RateLimit(RateLimitPolicy.Auth)]
    public async Task<IActionResult> ConfirmEmailChange(string? token)
    {
        var result = token is null ? RepositoryResult.NotFound : await accounts.ConfirmEmailChangeAsync(token);
        if (result == RepositoryResult.Success)
        {
            await auth.SignOutAsync(HttpContext);
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

        var userId = User.GetUserId();
        if (!await accounts.VerifyPasswordAsync(userId, model.Password))
        {
            ViewBag.Message = Messages.WrongPassword;
            return View(model);
        }

        // El sello rota y cierra las otras sesiones; la actual se reemite con el sello nuevo.
        ViewBag.Message = await accounts.ChangePasswordAsync(userId, model.NewPassword)
            ? await RefreshClaimsAsync()
            : Messages.UpdateError;
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

        var userId = User.GetUserId();
        if (!await accounts.VerifyPasswordAsync(userId, model.Password))
        {
            ViewBag.Message = Messages.WrongPassword;
            return View(model);
        }

        if (await accounts.DeleteAccountAsync(userId))
        {
            await auth.SignOutAsync(HttpContext);
            TempData["Message"] = Messages.AccountDeleted;
            return RedirectToAction("Index", "User");
        }

        ViewBag.Message = Messages.AccountDeleteError;
        return View(model);
    }

    private async Task<string> RefreshClaimsAsync()
    {
        await auth.RefreshClaimsAsync(HttpContext, User.GetUserId());
        return Messages.UpdateOk;
    }
}
