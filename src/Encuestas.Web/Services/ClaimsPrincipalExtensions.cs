using System.Globalization;
using System.Security.Claims;

namespace Encuestas.Web.Services;

/// <summary>Lectura de los claims que emite <see cref="AuthService.SignInAsync"/>.</summary>
public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!, CultureInfo.InvariantCulture);

    public static string GetEmail(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

    public static string GetGivenName(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.GivenName) ?? string.Empty;
}
