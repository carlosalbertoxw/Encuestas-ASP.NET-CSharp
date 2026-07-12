using Microsoft.Extensions.Diagnostics.HealthChecks;
using MySqlConnector;

namespace Encuestas.Web.Infrastructure;

/// <summary>Comprueba la conectividad con MySQL para el endpoint <c>/health</c> (CAL-03).</summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly MySqlDataSource _dataSource;

    public DatabaseHealthCheck(MySqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync(cancellationToken);
            return HealthCheckResult.Healthy("Base de datos accesible.");
        }
        catch (MySqlException ex)
        {
            return HealthCheckResult.Unhealthy("No se puede conectar con la base de datos.", ex);
        }
    }
}
