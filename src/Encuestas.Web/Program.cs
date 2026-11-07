using System.IO.Compression;
using System.Threading.RateLimiting;
using Encuestas.Data;
using Encuestas.Web.Infrastructure;
using Encuestas.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using MySqlConnector;

// `dotnet Encuestas.Web.dll healthcheck`: sonda del HEALTHCHECK de Docker. La imagen chiseled
// no trae curl ni shell, así que la propia aplicación consulta su endpoint de liveness.
if (args is ["healthcheck"])
{
    return await LocalHealthCheck.RunAsync();
}

var builder = WebApplication.CreateBuilder(args);

// Fuera de desarrollo, logs en JSON (una línea por evento, con el RequestId/TraceId del scope).
if (!builder.Environment.IsDevelopment())
{
    builder.Logging.ClearProviders();
    builder.Logging.AddJsonConsole(options =>
    {
        options.IncludeScopes = true;
        options.UseUtcTimestamp = true;
    });
}

builder.Services.AddControllersWithViews();

var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Falta la cadena de conexión 'Default' (appsettings.json o variable de entorno ConnectionStrings__Default).");
}
builder.Services.AddMySqlDataSource(connectionString);

// Claves de Data Protection (cookies, antiforgery y tokens de correo). Si se indica un
// directorio se persisten ahí; en Docker es un volumen, para que sobrevivan a los reinicios.
var dataProtection = builder.Services.AddDataProtection().SetApplicationName("Encuestas");
if (builder.Configuration["DataProtection:KeysPath"] is { Length: > 0 } keysPath)
{
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
}

builder.Services.AddMemoryCache();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPollRepository, PollRepository>();
builder.Services.AddScoped<IAnswerRepository, AnswerRepository>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddSingleton<PasswordService>();
builder.Services.AddSingleton<SecurityStampCache>();
builder.Services.AddSingleton<AccountLockout>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddSingleton<AppUrls>();
AddEmailSender(builder);

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
        // Rechaza la cookie si su sello de seguridad no coincide con el de la BD
        // (p. ej. tras un cambio de contraseña en otra sesión). El sello se cachea.
        options.Events.OnValidatePrincipal = SecurityStampValidator.ValidateAsync;
    });
// Sesiones guardadas en MySQL: el logout las invalida en el servidor.
builder.Services.AddMySqlTicketStore();

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

builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

// Tras un reverse proxy (nginx, Traefik...), estos encabezados permiten reconocer el esquema
// https original y la IP real del cliente (que usa el rate limiting). Por defecto solo se confía
// en loopback; en producción agrega la red del proxy a KnownIPNetworks o KnownProxies.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

var app = builder.Build();

// Falla al arrancar (y no al enviar el primer correo) si falta la URL pública.
_ = app.Services.GetRequiredService<AppUrls>();

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
    var headers = context.Response.Headers;
    headers["X-Request-Id"] = context.TraceIdentifier;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    // Sin scripts en línea; 'unsafe-inline' en estilos sigue por los estilos que
    // Bootstrap aplica dinámicamente a los menús desplegables.
    headers.ContentSecurityPolicy =
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

// Liveness: solo confirma que el proceso responde (sin BD, para no reiniciar la app en bucle
// si cae MySQL). Readiness: además comprueba la base de datos.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") });

app.Run();
return 0;

// Los correos siempre salen por SMTP (en Development, a Mailpit según appsettings.Development.json);
// nunca se escriben en el log, porque llevan enlaces con tokens.
static void AddEmailSender(WebApplicationBuilder builder)
{
    var smtp = builder.Configuration.GetSection(SmtpOptions.SectionName).Get<SmtpOptions>() ?? new SmtpOptions();
    if (string.IsNullOrWhiteSpace(smtp.Host))
    {
        throw new InvalidOperationException("Falta Smtp:Host (variable de entorno Smtp__Host) para enviar los correos.");
    }
    builder.Services.AddSingleton(smtp);
    builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
}

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

file static class LocalHealthCheck
{
    public static async Task<int> RunAsync()
    {
        var port = Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS")?.Split(';')[0] ?? "8080";
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
        try
        {
            using var response = await client.GetAsync(new Uri($"http://localhost:{port}/health/live"));
            return response.IsSuccessStatusCode ? 0 : 1;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return 1;
        }
    }
}
