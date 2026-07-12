using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
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

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        Environment.SetEnvironmentVariable(ConnStringEnvVar, _container.GetConnectionString());
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        Environment.SetEnvironmentVariable(ConnStringEnvVar, null);
        await _container.DisposeAsync();
        await DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
    }
}

[CollectionDefinition("http")]
public class HttpCollection : ICollectionFixture<WebAppFactory>;
