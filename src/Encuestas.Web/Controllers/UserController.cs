using System.Security.Claims;
using Encuestas.Data;
using Encuestas.Model;
using Encuestas.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Encuestas.Web.Controllers;

public class UserController : Controller
{
    private readonly IUserRepository _users;
    private readonly IPollRepository _polls;
    private readonly PasswordService _passwords;

    public UserController(IUserRepository users, IPollRepository polls, PasswordService passwords)
    {
        _users = users;
        _polls = polls;
        _passwords = passwords;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Página de inicio: acceso/registro para visitantes, o perfil público si se pasa ?profile=.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Index(string? profile)
    {
        if (!User.Identity!.IsAuthenticated)
        {
            ViewBag.Message = TempData["Message"];
            ViewData["Title"] = "Inicio";
            return View();
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

    /// <summary>Procesa los formularios de acceso (sign-in) y registro (sign-up) de la página de inicio.</summary>
    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(string? form, string? email, string? password, string? rePassword)
    {
        if (User.Identity!.IsAuthenticated)
        {
            return RedirectToAction("Index", "Poll");
        }

        ViewData["Title"] = "Inicio";

        if (form == "sign-in")
        {
            if (IsValidField(email, 50) && IsValidField(password, 50))
            {
                var userProfile = await _users.GetProfileByEmailAsync(email!);
                var outcome = userProfile is null
                    ? PasswordVerificationOutcome.Failed
                    : _passwords.Verify(userProfile.User.PasswordHash, password!);

                if (outcome != PasswordVerificationOutcome.Failed)
                {
                    if (outcome == PasswordVerificationOutcome.SuccessRehashNeeded)
                    {
                        await _users.UpdatePasswordAsync(userProfile!.User.Id, _passwords.Hash(password!));
                    }
                    await SignInUserAsync(userProfile!);
                    return RedirectToAction("Index", "Poll");
                }
            }
            ViewBag.Message = "Correo o contraseña incorrectos";
        }
        else if (form == "sign-up")
        {
            if (IsValidField(email, 50) && IsValidField(password, 50) && password == rePassword)
            {
                if (await _users.CreateUserAsync(email!, _passwords.Hash(password!)))
                {
                    ViewBag.Message = "El registro se realizó exitosamente";
                }
                else
                {
                    ViewBag.Message = "El correo electrónico ya está registrado";
                }
            }
            else
            {
                ViewBag.Message = "Error en la validación de los datos";
            }
        }

        return View();
    }

    [HttpGet]
    [Authorize]
    public IActionResult EditProfile()
    {
        ViewData["Title"] = "Editar perfil";
        return View();
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProfile(string? name)
    {
        ViewData["Title"] = "Editar perfil";
        if (!IsValidField(name, 50))
        {
            ViewBag.Message = "Ocurrió un error en la validación de los datos";
            return View();
        }

        if (await _users.UpdateNameAsync(CurrentUserId, name!))
        {
            await RefreshClaimsAsync();
            ViewBag.Message = "Los datos se actualizaron exitosamente";
        }
        else
        {
            ViewBag.Message = "Ocurrió un error al actualizar los datos";
        }
        return View();
    }

    [HttpGet]
    [Authorize]
    public IActionResult ChangeUser()
    {
        ViewData["Title"] = "Cambiar usuario";
        return View();
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeUser(string? userName, string? password)
    {
        ViewData["Title"] = "Cambiar usuario";
        if (!IsValidField(userName, 25) || !IsValidField(password, 50))
        {
            ViewBag.Message = "Ocurrió un error en la validación de los datos";
            return View();
        }

        if (!await VerifyCurrentPasswordAsync(password!))
        {
            ViewBag.Message = "La contraseña es incorrecta";
            return View();
        }

        if (await _users.UpdateUserNameAsync(CurrentUserId, userName!))
        {
            await RefreshClaimsAsync();
            ViewBag.Message = "Los datos se actualizaron exitosamente";
        }
        else
        {
            ViewBag.Message = "El nombre de usuario no está disponible";
        }
        return View();
    }

    [HttpGet]
    [Authorize]
    public IActionResult ChangeEmail()
    {
        ViewData["Title"] = "Cambiar correo";
        return View();
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeEmail(string? email, string? password)
    {
        ViewData["Title"] = "Cambiar correo";
        if (!IsValidField(email, 50) || !IsValidField(password, 50))
        {
            ViewBag.Message = "Ocurrió un error en la validación de los datos";
            return View();
        }

        if (!await VerifyCurrentPasswordAsync(password!))
        {
            ViewBag.Message = "La contraseña es incorrecta";
            return View();
        }

        if (await _users.UpdateEmailAsync(CurrentUserId, email!))
        {
            await RefreshClaimsAsync();
            ViewBag.Message = "Los datos se actualizaron exitosamente";
        }
        else
        {
            ViewBag.Message = "El correo electrónico ya está registrado";
        }
        return View();
    }

    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword()
    {
        ViewData["Title"] = "Cambiar contraseña";
        return View();
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(string? newPassword, string? reNewPassword, string? password)
    {
        ViewData["Title"] = "Cambiar contraseña";
        if (!IsValidField(newPassword, 50) || newPassword != reNewPassword || !IsValidField(password, 50))
        {
            ViewBag.Message = "Ocurrió un error en la validación de los datos";
            return View();
        }

        if (!await VerifyCurrentPasswordAsync(password!))
        {
            ViewBag.Message = "La contraseña es incorrecta";
            return View();
        }

        if (await _users.UpdatePasswordAsync(CurrentUserId, _passwords.Hash(newPassword!)))
        {
            ViewBag.Message = "Los datos se actualizaron exitosamente";
        }
        else
        {
            ViewBag.Message = "Ocurrió un error al actualizar los datos";
        }
        return View();
    }

    [HttpGet]
    [Authorize]
    public IActionResult DeleteAccount()
    {
        ViewData["Title"] = "Borrar cuenta";
        return View();
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAccount(string? password)
    {
        ViewData["Title"] = "Borrar cuenta";
        if (!IsValidField(password, 50))
        {
            ViewBag.Message = "Ocurrió un error en la validación de los datos";
            return View();
        }

        if (!await VerifyCurrentPasswordAsync(password!))
        {
            ViewBag.Message = "La contraseña es incorrecta";
            return View();
        }

        if (await _users.DeleteAccountAsync(CurrentUserId))
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["Message"] = "La cuenta se eliminó exitosamente";
            return RedirectToAction("Index", "User");
        }

        ViewBag.Message = "Ocurrió un error al eliminar la cuenta";
        return View();
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> CloseSession()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "User");
    }

    private static bool IsValidField(string? value, int maxLength) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= maxLength;

    private async Task<bool> VerifyCurrentPasswordAsync(string password)
    {
        var user = await _users.GetUserAsync(CurrentUserId);
        return user is not null && _passwords.Verify(user.PasswordHash, password) != PasswordVerificationOutcome.Failed;
    }

    private async Task SignInUserAsync(UserProfile profile)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, profile.User.Id.ToString()),
            new(ClaimTypes.Name, profile.UserName),
            new(ClaimTypes.GivenName, profile.Name),
            new(ClaimTypes.Email, profile.User.Email)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    }

    /// <summary>Reemite la cookie con los datos actuales tras editar perfil, usuario o correo.</summary>
    private async Task RefreshClaimsAsync()
    {
        var user = await _users.GetUserAsync(CurrentUserId);
        if (user is null)
        {
            return;
        }
        var profile = await _users.GetProfileByEmailAsync(user.Email);
        if (profile is not null)
        {
            await SignInUserAsync(profile);
        }
    }
}
