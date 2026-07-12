using System.IO.Compression;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Encuestas.Data;
using Encuestas.Web.Infrastructure;
using Encuestas.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Falta la cadena de conexión 'Default' (appsettings.json o variable de entorno ConnectionStrings__Default).");
}
builder.Services.AddMySqlDataSource(connectionString);

builder.Services.AddMemoryCache();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPollRepository, PollRepository>();
builder.Services.AddScoped<IAnswerRepository, AnswerRepository>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddSingleton<PasswordService>();
builder.Services.AddSingleton<SecurityStampCache>();
builder.Services.AddSingleton<AccountLockout>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<IEmailSender, LoggingEmailSender>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/User/Index";
        options.AccessDeniedPath = "/User/Index";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "Encuestas.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        // En desarrollo la app corre sobre http; fuera de él la cookie solo viaja por HTTPS.
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        // SEG-05: rechaza la cookie si su sello de seguridad no coincide con el de la BD
        // (p. ej. tras un cambio de contraseña en otra sesión). El sello se cachea (REN-01).
        options.Events.OnValidatePrincipal = SecurityStampValidator.ValidateAsync;
    });

// Limita los intentos de login/registro por IP para frenar fuerza bruta y registro masivo.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "desconocido",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
        await context.HttpContext.Response.WriteAsync(
            "Demasiados intentos. Espera un minuto y vuelve a intentarlo.", cancellationToken);
    };
});

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);

builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database");

// Tras un reverse proxy (nginx, Traefik...), estos encabezados permiten reconocer el esquema
// https original. En producción configura también KnownProxies/KnownNetworks.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

var app = builder.Build();

// Aplica las migraciones pendientes (src/Encuestas.Web/Migrations) antes de aceptar tráfico.
MigrationRunner.Run(connectionString, app.Logger);
if (app.Environment.IsDevelopment())
{
    await DevDataSeeder.SeedAsync(app.Services, app.Logger);
}

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Error/NotFound");

app.UseResponseCompression();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    // Sin scripts en línea (SEG-05); 'unsafe-inline' en estilos sigue por los estilos que
    // Bootstrap aplica dinámicamente a los menús desplegables.
    context.Response.Headers["Content-Security-Policy"] =
        "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; frame-ancestors 'none'";
    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=User}/{action=Index}/{id?}");
app.MapHealthChecks("/health");

app.Run();

// Necesario para que WebApplicationFactory pueda referenciar el punto de entrada en las pruebas.
public partial class Program;

file static class SecurityStampValidator
{
    public static async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        var principal = context.Principal;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var idClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var stampClaim = principal.FindFirst(AuthService.SecurityStampClaimType)?.Value;
        if (!int.TryParse(idClaim, out var userId))
        {
            context.RejectPrincipal();
            return;
        }

        var services = context.HttpContext.RequestServices;
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
