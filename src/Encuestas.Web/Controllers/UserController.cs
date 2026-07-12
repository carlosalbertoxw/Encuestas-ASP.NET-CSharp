using System.Globalization;
using System.Security.Claims;
using Encuestas.Data;
using Encuestas.Web.Models;
using Encuestas.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Encuestas.Web.Controllers;

public class UserController : Controller
{
    private readonly IUserRepository _users;
    private readonly IPollRepository _polls;
    private readonly PasswordService _passwords;
    private readonly AuthService _auth;
    private readonly TokenService _tokens;
    private readonly IEmailSender _email;
    private readonly SecurityStampCache _stampCache;
    private readonly ILogger<UserController> _logger;

    public UserController(IUserRepository users, IPollRepository polls, PasswordService passwords,
        AuthService auth, TokenService tokens, IEmailSender email, SecurityStampCache stampCache,
        ILogger<UserController> logger)
    {
        _users = users;
        _polls = polls;
        _passwords = passwords;
        _auth = auth;
        _tokens = tokens;
        _email = email;
        _stampCache = stampCache;
        _logger = logger;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, CultureInfo.InvariantCulture);

    /// <summary>Página de inicio: acceso/registro para visitantes, o perfil público si se pasa ?profile=.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Index(string? profile)
    {
        if (!User.Identity!.IsAuthenticated)
        {
            ViewBag.Message = TempData["Message"];
            ViewData["Title"] = "Inicio";
            return View(new HomeViewModel());
        }

        if (profile is null)
        {
            return RedirectToAction("Index", "Poll");
        }

        var userProfile = await _users.GetProfileByUserNameAsync(profile);
        if (userProfile is null)
        {
            return NotFound();
        }

        ViewBag.Message = TempData["Message"];
        ViewData["Title"] = $"{userProfile.Name} ({userProfile.UserName})";
        var polls = await _polls.GetPollsAsync(userProfile.User.Id);
        return View("Profile", polls);
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

        ViewData["Title"] = "Inicio";
        if (!ModelState.IsValid)
        {
            ViewBag.Message = Messages.InvalidCredentials;
            return View("Index", new HomeViewModel { Login = model });
        }

        var result = await _auth.LoginAsync(HttpContext, model.Email, model.Password);
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

        ViewData["Title"] = "Inicio";
        if (!ModelState.IsValid)
        {
            return View("Index", new HomeViewModel { Register = model });
        }

        var result = await _users.CreateUserAsync(model.Email, _passwords.Hash(model.Password), NewSecurityStamp());
        if (result == RepositoryResult.Success)
        {
            var profile = await _users.GetProfileByEmailAsync(model.Email);
            if (profile is not null)
            {
                await SendConfirmationEmailAsync(model.Email, profile.User.Id);
            }
            _logger.LogInformation("Cuenta nueva registrada para {Email}", model.Email);
        }
        // SEG-03: se responde lo mismo exista o no el correo, para no revelar cuentas.
        ViewBag.Message = Messages.RegisterAcknowledged;
        return View("Index", new HomeViewModel());
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(string? token)
    {
        var userId = token is null ? null : _tokens.ValidateEmailConfirmationToken(token);
        if (userId is null)
        {
            TempData["Message"] = Messages.EmailConfirmationInvalid;
        }
        else
        {
            await _users.ConfirmEmailAsync(userId.Value);
            _logger.LogInformation("Correo confirmado para el usuario {UserId}", userId.Value);
            TempData["Message"] = Messages.EmailConfirmed;
        }
        return RedirectToAction("Index");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        ViewData["Title"] = "Recuperar contraseña";
        return View(new ForgotPasswordViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        ViewData["Title"] = "Recuperar contraseña";
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var profile = await _users.GetProfileByEmailAsync(model.Email);
        if (profile is not null)
        {
            var token = _tokens.CreatePasswordResetToken(profile.User.Id, profile.User.SecurityStamp);
            var link = Url.Action("ResetPassword", "User", new { token }, Request.Scheme)!;
            await _email.SendAsync(model.Email, "Restablece tu contraseña",
                $"Para restablecer tu contraseña visita: <a href=\"{link}\">{link}</a>");
            _logger.LogInformation("Enlace de restablecimiento generado para el usuario {UserId}", profile.User.Id);
        }
        // SEG-03: mensaje genérico para no revelar si el correo existe.
        ViewBag.Message = Messages.PasswordResetSent;
        return View(new ForgotPasswordViewModel());
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string? token)
    {
        ViewData["Title"] = "Restablecer contraseña";
        if (token is null || _tokens.ValidatePasswordResetToken(token) is null)
        {
            ViewBag.Message = Messages.PasswordResetInvalid;
            return View(new ResetPasswordViewModel());
        }
        return View(new ResetPasswordViewModel { Token = token });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        ViewData["Title"] = "Restablecer contraseña";
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var parsed = _tokens.ValidatePasswordResetToken(model.Token);
        // El token incluye el sello de seguridad: si no coincide con el actual, ya se usó o caducó.
        if (parsed is not { } valid || await _users.GetSecurityStampAsync(valid.UserId) is not { } stamp || stamp != valid.SecurityStamp)
        {
            ViewBag.Message = Messages.PasswordResetInvalid;
            return View(model);
        }

        var result = await _users.ChangePasswordAsync(valid.UserId, _passwords.Hash(model.NewPassword), NewSecurityStamp());
        if (result == RepositoryResult.Success)
        {
            _stampCache.Invalidate(valid.UserId);
            _logger.LogInformation("Contraseña restablecida para el usuario {UserId}", valid.UserId);
            TempData["Message"] = Messages.PasswordResetOk;
            return RedirectToAction("Index");
        }

        ViewBag.Message = Messages.UpdateError;
        return View(model);
    }

    [HttpGet]
    [Authorize]
    public IActionResult EditProfile()
    {
        ViewData["Title"] = "Editar perfil";
        return View(new EditProfileViewModel { Name = User.FindFirstValue(ClaimTypes.GivenName) ?? string.Empty });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProfile(EditProfileViewModel model)
    {
        ViewData["Title"] = "Editar perfil";
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (await _users.UpdateNameAsync(CurrentUserId, model.Name) == RepositoryResult.Success)
        {
            await _auth.RefreshClaimsAsync(HttpContext, CurrentUserId);
            ViewBag.Message = Messages.UpdateOk;
        }
        else
        {
            ViewBag.Message = Messages.UpdateError;
        }
        return View(model);
    }

    [HttpGet]
    [Authorize]
    public IActionResult ChangeUser()
    {
        ViewData["Title"] = "Cambiar usuario";
        return View(new ChangeUserViewModel { UserName = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeUser(ChangeUserViewModel model)
    {
        ViewData["Title"] = "Cambiar usuario";
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!await VerifyCurrentPasswordAsync(model.Password))
        {
            ViewBag.Message = Messages.WrongPassword;
            return View(model);
        }

        ViewBag.Message = await _users.UpdateUserNameAsync(CurrentUserId, model.UserName) switch
        {
            RepositoryResult.Success => await RefreshAndConfirmAsync(),
            RepositoryResult.Duplicate => Messages.UserNameTaken,
            _ => Messages.UpdateError
        };
        return View(model);
    }

    [HttpGet]
    [Authorize]
    public IActionResult ChangeEmail()
    {
        ViewData["Title"] = "Cambiar correo";
        return View(new ChangeEmailViewModel { Email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeEmail(ChangeEmailViewModel model)
    {
        ViewData["Title"] = "Cambiar correo";
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!await VerifyCurrentPasswordAsync(model.Password))
        {
            ViewBag.Message = Messages.WrongPassword;
            return View(model);
        }

        // SEG-03: no distinguir "correo ya registrado" para no revelar cuentas ajenas.
        ViewBag.Message = await _users.UpdateEmailAsync(CurrentUserId, model.Email) switch
        {
            RepositoryResult.Success => await RefreshAndConfirmAsync(logEmailChange: true),
            _ => Messages.UpdateError
        };
        return View(model);
    }

    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword()
    {
        ViewData["Title"] = "Cambiar contraseña";
        return View(new ChangePasswordViewModel());
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        ViewData["Title"] = "Cambiar contraseña";
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
        var result = await _users.ChangePasswordAsync(CurrentUserId, _passwords.Hash(model.NewPassword), NewSecurityStamp());
        if (result == RepositoryResult.Success)
        {
            await _auth.RefreshClaimsAsync(HttpContext, CurrentUserId);
            _stampCache.Invalidate(CurrentUserId);
            _logger.LogInformation("Contraseña cambiada por el usuario {UserId}", CurrentUserId);
            ViewBag.Message = Messages.UpdateOk;
        }
        else
        {
            ViewBag.Message = Messages.UpdateError;
        }
        return View(new ChangePasswordViewModel());
    }

    [HttpGet]
    [Authorize]
    public IActionResult DeleteAccount()
    {
        ViewData["Title"] = "Borrar cuenta";
        return View(new DeleteAccountViewModel());
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAccount(DeleteAccountViewModel model)
    {
        ViewData["Title"] = "Borrar cuenta";
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (!await VerifyCurrentPasswordAsync(model.Password))
        {
            ViewBag.Message = Messages.WrongPassword;
            return View(model);
        }

        var userId = CurrentUserId;
        if (await _users.DeleteAccountAsync(userId) == RepositoryResult.Success)
        {
            await _auth.SignOutAsync(HttpContext);
            _stampCache.Invalidate(userId);
            _logger.LogInformation("Cuenta eliminada por el usuario {UserId}", userId);
            TempData["Message"] = Messages.AccountDeleted;
            return RedirectToAction("Index", "User");
        }

        ViewBag.Message = Messages.AccountDeleteError;
        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CloseSession()
    {
        await _auth.SignOutAsync(HttpContext);
        return RedirectToAction("Index", "User");
    }

    private static string NewSecurityStamp() => Guid.NewGuid().ToString();

    private async Task SendConfirmationEmailAsync(string email, int userId)
    {
        var token = _tokens.CreateEmailConfirmationToken(userId);
        var link = Url.Action("ConfirmEmail", "User", new { token }, Request.Scheme)!;
        await _email.SendAsync(email, "Confirma tu cuenta",
            $"Para confirmar tu cuenta visita: <a href=\"{link}\">{link}</a>");
    }

    private async Task<bool> VerifyCurrentPasswordAsync(string password)
    {
        var user = await _users.GetUserAsync(CurrentUserId);
        return user is not null && _passwords.Verify(user.PasswordHash, password) != PasswordVerificationOutcome.Failed;
    }

    private async Task<string> RefreshAndConfirmAsync(bool logEmailChange = false)
    {
        await _auth.RefreshClaimsAsync(HttpContext, CurrentUserId);
        if (logEmailChange)
        {
            _logger.LogInformation("Correo actualizado para el usuario {UserId}", CurrentUserId);
        }
        return Messages.UpdateOk;
    }
}
