using Microsoft.AspNetCore.Hosting;
using Encuestas.Web.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.MySql;

namespace Encuestas.Tests;

/// <summary>
/// Arranca la aplicación real (Program.cs) contra un MySQL efímero de Testcontainers, en el
/// entorno Development para que se apliquen las migraciones y los datos de demostración.
/// La cadena de conexión se inyecta por variable de entorno porque Program.cs la lee muy
/// temprano (antes de que apliquen los overrides de ConfigureAppConfiguration).
/// </summary>
public class WebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string ConnStringEnvVar = "ConnectionStrings__Default";

    private readonly MySqlContainer _container = new MySqlBuilder("mysql:8.4")
        .WithDatabase("encuestas_http")
        .Build();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        Environment.SetEnvironmentVariable(ConnStringEnvVar, _container.GetConnectionString());
    }

    public override async ValueTask DisposeAsync()
    {
        Environment.SetEnvironmentVariable(ConnStringEnvVar, null);
        await base.DisposeAsync();
        await _container.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        // Todas las pruebas comparten IP: se amplían los límites para que no se bloqueen entre sí.
        builder.UseSetting("RateLimiting:AuthPermitLimit", "10000");
        builder.UseSetting("RateLimiting:EmailChangePermitLimit", "10000");
        // Las pruebas no envían correos: se capturan en memoria en lugar de ir a Mailpit.
        builder.ConfigureTestServices(services => services.Replace(ServiceDescriptor.Singleton<IEmailSender>(Emails)));
    }

    /// <summary>Correos que la aplicación habría enviado durante las pruebas.</summary>
    public CapturingEmailSender Emails { get; } = new();
}

/// <summary>Emisor de correo de prueba: guarda los mensajes en lugar de enviarlos.</summary>
public sealed class CapturingEmailSender : IEmailSender
{
    private readonly System.Collections.Concurrent.ConcurrentQueue<(string To, string Subject, string Body)> _sent = new();

    public IReadOnlyCollection<(string To, string Subject, string Body)> Sent => _sent;

    /// <summary>
    /// Espera a que el despachador en segundo plano entregue <paramref name="count"/> correos con
    /// ese destinatario y asunto, y devuelve el cuerpo del último.
    /// </summary>
    public async Task<string> WaitForAsync(string to, string subject, int count = 1)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (DateTime.UtcNow < deadline)
        {
            var matches = _sent.Where(m => m.To == to && m.Subject == subject).ToList();
            if (matches.Count >= count)
            {
                return matches[^1].Body;
            }
            await Task.Delay(100);
        }
        throw new TimeoutException($"No llegó el correo '{subject}' a {to}.");
    }

    public Task SendAsync(string recipient, string subject, string htmlBody)
    {
        _sent.Enqueue((recipient, subject, htmlBody));
        return Task.CompletedTask;
    }
}

[CollectionDefinition("http")]
public class HttpCollection : ICollectionFixture<WebAppFactory>;
