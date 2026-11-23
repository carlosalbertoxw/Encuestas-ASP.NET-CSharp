using Encuestas.Data;
using Encuestas.Web.Infrastructure;
using Encuestas.Web.Models;
using Encuestas.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Encuestas.Web.Controllers;

/// <summary>
/// Página de inicio, perfiles públicos y flujos de acceso: inicio de sesión, registro,
/// confirmación de correo, recuperación de contraseña y cierre de sesión. La gestión de la
/// cuenta autenticada vive en <see cref="AccountController"/>.
/// </summary>
public class UserController(IUserRepository users, IPollRepository polls, AuthService auth, AccountService accounts)
    : Controller
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
    [RateLimit(RateLimitPolicy.Auth)]
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
    [RateLimit(RateLimitPolicy.Auth)]
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

        await accounts.RegisterAsync(model.Email, model.Password);
        // Se responde lo mismo exista o no el correo, para no revelar cuentas.
        ViewBag.Message = Messages.RegisterAcknowledged;
        return View("Index", new HomeViewModel());
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(string? token)
    {
        TempData["Message"] = token is not null && await accounts.ConfirmEmailAsync(token)
            ? Messages.EmailConfirmed
            : Messages.EmailConfirmationInvalid;
        return RedirectToAction("Index");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResendConfirmation() => View(new ResendConfirmationViewModel());

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [RateLimit(RateLimitPolicy.Auth)]
    public async Task<IActionResult> ResendConfirmation(ResendConfirmationViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await accounts.ResendConfirmationAsync(model.Email);
        // Mensaje genérico para no revelar si el correo existe ni si ya estaba confirmado.
        ViewBag.Message = Messages.ConfirmationResent;
        return View(new ResendConfirmationViewModel());
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [RateLimit(RateLimitPolicy.Auth)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await accounts.RequestPasswordResetAsync(model.Email);
        // Mensaje genérico para no revelar si el correo existe.
        ViewBag.Message = Messages.PasswordResetSent;
        return View(new ForgotPasswordViewModel());
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string? token)
    {
        if (token is null || !accounts.IsPasswordResetTokenValid(token))
        {
            ViewBag.Message = Messages.PasswordResetInvalid;
            return View(new ResetPasswordViewModel());
        }
        return View(new ResetPasswordViewModel { Token = token });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [RateLimit(RateLimitPolicy.Auth)]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        switch (await accounts.ResetPasswordAsync(model.Token, model.NewPassword))
        {
            case ResetPasswordResult.Success:
                TempData["Message"] = Messages.PasswordResetOk;
                return RedirectToAction("Index");
            case ResetPasswordResult.InvalidToken:
                ViewBag.Message = Messages.PasswordResetInvalid;
                return View(model);
            default:
                ViewBag.Message = Messages.UpdateError;
                return View(model);
        }
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseSession()
    {
        await auth.SignOutAsync(HttpContext);
        return RedirectToAction("Index");
    }
}
