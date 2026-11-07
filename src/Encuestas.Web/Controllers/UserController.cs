using Encuestas.Data;
using Encuestas.Model;
using Encuestas.Web.Models;
using Encuestas.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Encuestas.Web.Controllers;

/// <summary>
/// Página de inicio, perfiles públicos y flujos de acceso: inicio de sesión, registro,
/// confirmación de correo, recuperación de contraseña y cierre de sesión. La gestión de la
/// cuenta autenticada vive en <see cref="AccountController"/>.
/// </summary>
public class UserController(
    IUserRepository users,
    IPollRepository polls,
    PasswordService passwords,
    AuthService auth,
    TokenService tokens,
    IEmailSender email,
    AppUrls urls,
    SecurityStampCache stampCache,
    ILogger<UserController> logger) : Controller
{
    /// <summary>Página de inicio: acceso/registro para visitantes, o perfil público si se pasa ?profile=.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Index(string? profile, int page = 1)
    {
        ViewBag.Message = TempData["Message"];
        if (!User.Identity!.IsAuthenticated)
        {
            return View(new HomeViewModel());
        }

        if (profile is null)
        {
            return RedirectToAction("Index", "Poll");
        }

        var userProfile = await users.GetProfileByUserNameAsync(profile);
        if (userProfile is null)
        {
            return NotFound();
        }

        var userPolls = await polls.GetPollsAsync(userProfile.User.Id, page, Paging.DefaultPageSize);
        return View("Profile", new ProfileViewModel(userProfile, userPolls));
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([Bind(Prefix = nameof(HomeViewModel.Login))] LoginViewModel model)
    {
        if (User.Identity!.IsAuthenticated)
        {
            return RedirectToAction("Index", "Poll");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Message = Messages.InvalidCredentials;
            return View("Index", new HomeViewModel { Login = model });
        }

        var result = await auth.LoginAsync(HttpContext, model.Email, model.Password);
        if (result == LoginResult.Success)
        {
            return RedirectToAction("Index", "Poll");
        }

        ViewBag.Message = result switch
        {
            LoginResult.EmailNotConfirmed => Messages.EmailNotConfirmed,
            LoginResult.LockedOut => Messages.AccountLocked,
            _ => Messages.InvalidCredentials
        };
        return View("Index", new HomeViewModel { Login = model });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Register([Bind(Prefix = nameof(HomeViewModel.Register))] RegisterViewModel model)
    {
        if (User.Identity!.IsAuthenticated)
        {
            return RedirectToAction("Index", "Poll");
        }

        if (!ModelState.IsValid)
        {
            return View("Index", new HomeViewModel { Register = model });
        }

        var result = await users.CreateUserAsync(model.Email, passwords.Hash(model.Password), NewSecurityStamp());
        if (result == RepositoryResult.Success && await users.GetProfileByEmailAsync(model.Email) is { } profile)
        {
            var link = urls.Absolute(Url.Action("ConfirmEmail", "User", new { token = tokens.CreateEmailConfirmationToken(profile.User.Id) })!);
            await email.SendAsync(model.Email, "Confirma tu cuenta", EmailTemplates.Link("Para confirmar tu cuenta visita:", link));
            logger.LogInformation("Cuenta nueva registrada: usuario {UserId}", profile.User.Id);
        }
        // Se responde lo mismo exista o no el correo, para no revelar cuentas.
        ViewBag.Message = Messages.RegisterAcknowledged;
        return View("Index", new HomeViewModel());
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(string? token)
    {
        var userId = token is null ? null : tokens.ValidateEmailConfirmationToken(token);
        if (userId is null)
        {
            TempData["Message"] = Messages.EmailConfirmationInvalid;
        }
        else
        {
            await users.ConfirmEmailAsync(userId.Value);
            logger.LogInformation("Correo confirmado para el usuario {UserId}", userId.Value);
            TempData["Message"] = Messages.EmailConfirmed;
        }
        return RedirectToAction("Index");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (await users.GetProfileByEmailAsync(model.Email) is { } profile)
        {
            var token = tokens.CreatePasswordResetToken(profile.User.Id, profile.User.SecurityStamp);
            var link = urls.Absolute(Url.Action("ResetPassword", "User", new { token })!);
            await email.SendAsync(model.Email, "Restablece tu contraseña",
                EmailTemplates.Link("Para restablecer tu contraseña visita:", link));
            logger.LogInformation("Enlace de restablecimiento generado para el usuario {UserId}", profile.User.Id);
        }
        // Mensaje genérico para no revelar si el correo existe.
        ViewBag.Message = Messages.PasswordResetSent;
        return View(new ForgotPasswordViewModel());
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string? token)
    {
        if (token is null || tokens.ValidatePasswordResetToken(token) is null)
        {
            ViewBag.Message = Messages.PasswordResetInvalid;
            return View(new ResetPasswordViewModel());
        }
        return View(new ResetPasswordViewModel { Token = token });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // El token incluye el sello de seguridad: si no coincide con el actual, ya se usó o caducó.
        if (tokens.ValidatePasswordResetToken(model.Token) is not { } valid
            || await users.GetSecurityStampAsync(valid.UserId) != valid.SecurityStamp)
        {
            ViewBag.Message = Messages.PasswordResetInvalid;
            return View(model);
        }

        if (await users.ChangePasswordAsync(valid.UserId, passwords.Hash(model.NewPassword), NewSecurityStamp()) == RepositoryResult.Success)
        {
            stampCache.Invalidate(valid.UserId);
            logger.LogInformation("Contraseña restablecida para el usuario {UserId}", valid.UserId);
            TempData["Message"] = Messages.PasswordResetOk;
            return RedirectToAction("Index");
        }

        ViewBag.Message = Messages.UpdateError;
        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseSession()
    {
        await auth.SignOutAsync(HttpContext);
        return RedirectToAction("Index");
    }

    internal static string NewSecurityStamp() => Guid.NewGuid().ToString();
}
