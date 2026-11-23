using Encuestas.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace Encuestas.Web.Infrastructure;

/// <summary>Políticas de límite de peticiones disponibles para <see cref="RateLimitAttribute"/>.</summary>
public enum RateLimitPolicy
{
    /// <summary>Formularios de acceso (login, registro, recuperación…), por IP.</summary>
    Auth,

    /// <summary>Solicitudes de cambio de correo, por usuario: cada una envía correos a terceros.</summary>
    EmailChange
}

/// <summary>Límites configurables (sección <c>RateLimiting</c>); las pruebas los amplían.</summary>
public class RateLimitOptions
{
    public const string SectionName = "RateLimiting";

    public int AuthPermitLimit { get; set; } = 10;
    public TimeSpan AuthWindow { get; set; } = TimeSpan.FromMinutes(1);
    public int EmailChangePermitLimit { get; set; } = 3;
    public TimeSpan EmailChangeWindow { get; set; } = TimeSpan.FromHours(1);
}

/// <summary>
/// Limita las peticiones de la acción con un contador de ventana fija en MySQL, compartido
/// entre réplicas. Al superar el límite responde 429.
/// </summary>
public sealed class RateLimitAttribute : TypeFilterAttribute
{
    public RateLimitAttribute(RateLimitPolicy policy) : base(typeof(RateLimitFilter)) => Arguments = [policy];
}

public sealed class RateLimitFilter(RateLimitPolicy policy, IAttemptCounterStore store, IOptions<RateLimitOptions> options)
    : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var limits = options.Value;
        var http = context.HttpContext;
        var (key, permitLimit, window) = policy switch
        {
            RateLimitPolicy.EmailChange => ($"email-change:{http.User.GetUserId()}", limits.EmailChangePermitLimit, limits.EmailChangeWindow),
            _ => ($"auth:{http.Connection.RemoteIpAddress?.ToString() ?? "desconocido"}", limits.AuthPermitLimit, limits.AuthWindow)
        };

        if (await store.CountAsync(key, window) >= permitLimit)
        {
            context.Result = new ContentResult
            {
                StatusCode = StatusCodes.Status429TooManyRequests,
                ContentType = "text/plain; charset=utf-8",
                Content = Messages.TooManyRequests
            };
            return;
        }

        await store.IncrementAsync(key, window);
        await next();
    }
}
