using Encuestas.Data;
using Encuestas.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Encuestas.Web.Infrastructure;

/// <summary>
/// Rechaza la cookie si su sello de seguridad no coincide con el de la BD (p. ej. tras un cambio
/// de contraseña en otra sesión). El sello se consulta a través de <see cref="SecurityStampCache"/>.
/// </summary>
public static class SecurityStampValidator
{
    public static async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        var principal = context.Principal;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var services = context.HttpContext.RequestServices;
        var stampClaim = principal.FindFirst(AuthService.SecurityStampClaimType)?.Value;
        int userId;
        try
        {
            userId = principal.GetUserId();
        }
        catch (Exception ex) when (ex is FormatException or ArgumentNullException)
        {
            context.RejectPrincipal();
            return;
        }

        var cache = services.GetRequiredService<SecurityStampCache>();
        var users = services.GetRequiredService<IUserRepository>();
        var currentStamp = await cache.GetAsync(userId, () => users.GetSecurityStampAsync(userId));
        if (currentStamp is null || currentStamp != stampClaim)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
