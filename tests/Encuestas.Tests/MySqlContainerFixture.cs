using Encuestas.Web.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using MySqlConnector;
using Testcontainers.MySql;

namespace Encuestas.Tests;

/// <summary>
/// Levanta un MySQL 8.4 efímero en Docker y le aplica las migraciones reales de la aplicación.
/// Se comparte entre todos los tests de la colección "mysql" (un solo contenedor por corrida).
/// </summary>
public sealed class MySqlContainerFixture : IAsyncLifetime
{
    private readonly MySqlContainer _container = new MySqlBuilder("mysql:8.4")
        .WithDatabase("encuestas_test")
        .Build();

    public MySqlDataSource DataSource { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        MigrationRunner.Run(_container.GetConnectionString(), NullLogger.Instance);
        DataSource = new MySqlDataSource(_container.GetConnectionString());
    }

    public async Task DisposeAsync()
    {
        await DataSource.DisposeAsync();
        await _container.DisposeAsync();
    }
}

[CollectionDefinition("mysql")]
public class MySqlCollection : ICollectionFixture<MySqlContainerFixture>;
