using Encuestas.Web.Infrastructure;
using Encuestas.Web.Models;
using Encuestas.Web.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

if (args is ["healthcheck"])
{
    return await LocalHealthCheck.RunAsync();
}

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Falta la cadena de conexión 'Default' (appsettings.json o variable de entorno ConnectionStrings__Default).");
}

builder.AddJsonLoggingOutsideDevelopment()
    .AddDataAccess(connectionString)
    .AddAccounts()
    .AddEmail()
    .AddHosting();
builder.Services.AddControllersWithViews(options =>
{
    // Mensajes del enlace de modelo en español (p. ej. un número vacío o mal escrito).
    var messages = options.ModelBindingMessageProvider;
    messages.SetValueMustNotBeNullAccessor(_ => ValidationMessages.Required);
    messages.SetMissingBindRequiredValueAccessor(_ => ValidationMessages.Required);
    messages.SetValueMustBeANumberAccessor(_ => ValidationMessages.Number);
    messages.SetAttemptedValueIsInvalidAccessor((_, _) => ValidationMessages.InvalidValue);
    messages.SetUnknownValueIsInvalidAccessor(_ => ValidationMessages.InvalidValue);
    messages.SetValueIsInvalidAccessor(_ => ValidationMessages.InvalidValue);
    messages.SetNonPropertyAttemptedValueIsInvalidAccessor(_ => ValidationMessages.InvalidValue);
    messages.SetNonPropertyUnknownValueIsInvalidAccessor(() => ValidationMessages.InvalidValue);
    messages.SetNonPropertyValueMustBeANumberAccessor(() => ValidationMessages.Number);
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
app.UseSecurityHeaders();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
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

// Necesario para que WebApplicationFactory pueda referenciar el punto de entrada en las pruebas.
public partial class Program;
