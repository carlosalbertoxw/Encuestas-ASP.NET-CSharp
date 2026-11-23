using System.IO.Compression;
using Encuestas.Data;
using Encuestas.Web.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using MySqlConnector;

namespace Encuestas.Web.Infrastructure;

/// <summary>Registro de servicios agrupado por tema, para que Program.cs se lea como un índice.</summary>
public static class ServiceRegistration
{
    /// <summary>Fuera de desarrollo, logs en JSON (una línea por evento, con el RequestId/TraceId del scope).</summary>
    public static WebApplicationBuilder AddJsonLoggingOutsideDevelopment(this WebApplicationBuilder builder)
    {
        if (!builder.Environment.IsDevelopment())
        {
            builder.Logging.ClearProviders();
            builder.Logging.AddJsonConsole(options =>
            {
                options.IncludeScopes = true;
                options.UseUtcTimestamp = true;
            });
        }
        return builder;
    }

    /// <summary>MySQL, repositorios, contadores de intentos y claves de Data Protection.</summary>
    public static WebApplicationBuilder AddDataAccess(this WebApplicationBuilder builder, string connectionString)
    {
        builder.Services.AddMySqlDataSource(connectionString);
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<IPollRepository, PollRepository>();
        builder.Services.AddScoped<IAnswerRepository, AnswerRepository>();
        builder.Services.AddSingleton<IAttemptCounterStore, MySqlAttemptCounterStore>();
        builder.Services.AddHostedService<DatabaseCleanupService>();

        // Claves de Data Protection (cookies, antiforgery y tokens de correo). Si se indica un
        // directorio se persisten ahí; en Docker es un volumen, para que sobrevivan a los reinicios.
        var dataProtection = builder.Services.AddDataProtection().SetApplicationName("Encuestas");
        if (builder.Configuration["DataProtection:KeysPath"] is { Length: > 0 } keysPath)
        {
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
        }
        return builder;
    }

    /// <summary>Cuentas: contraseñas, tokens, bloqueo, límites de peticiones y cookie con sesión en BD.</summary>
    public static WebApplicationBuilder AddAccounts(this WebApplicationBuilder builder)
    {
        var services = builder.Services;
        services.AddMemoryCache();
        services.AddScoped<AuthService>();
        services.AddScoped<AccountService>();
        services.AddSingleton<PasswordService>();
        services.AddSingleton<SecurityStampCache>();
        services.AddSingleton<AccountLockout>();
        services.AddSingleton<TokenService>();
        services.AddSingleton<AppUrls>();
        services.Configure<RateLimitOptions>(builder.Configuration.GetSection(RateLimitOptions.SectionName));

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
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
        services.AddMySqlTicketStore();
        return builder;
    }

    /// <summary>
    /// Correo: las peticiones encolan en <see cref="EmailOutbox"/> y <see cref="OutboxDispatcher"/>
    /// envía por SMTP (en Development, a Mailpit). Nunca se escriben en el log: llevan tokens.
    /// </summary>
    public static WebApplicationBuilder AddEmail(this WebApplicationBuilder builder)
    {
        var smtp = builder.Configuration.GetSection(SmtpOptions.SectionName).Get<SmtpOptions>() ?? new SmtpOptions();
        if (string.IsNullOrWhiteSpace(smtp.Host))
        {
            throw new InvalidOperationException("Falta Smtp:Host (variable de entorno Smtp__Host) para enviar los correos.");
        }
        builder.Services.AddSingleton(smtp);
        builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
        builder.Services.AddSingleton<EmailOutbox>();
        builder.Services.AddHostedService<OutboxDispatcher>();
        return builder;
    }

    /// <summary>Compresión gzip, sondas de salud y encabezados de proxy.</summary>
    public static WebApplicationBuilder AddHosting(this WebApplicationBuilder builder)
    {
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
        return builder;
    }

    /// <summary>Encabezados de seguridad y el identificador de la petición en cada respuesta.</summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
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
}
