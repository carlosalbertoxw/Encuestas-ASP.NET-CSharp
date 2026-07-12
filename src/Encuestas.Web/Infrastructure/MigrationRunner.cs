using DbUp;
using MySqlConnector;

namespace Encuestas.Web.Infrastructure;

/// <summary>
/// Aplica al arrancar los scripts SQL embebidos de la carpeta Migrations, en orden alfabético.
/// DbUp lleva el registro de lo ya aplicado en la tabla <c>schemaversions</c>, por lo que la
/// operación es idempotente y el esquema de cualquier entorno evoluciona de forma reproducible.
/// </summary>
public static class MigrationRunner
{
    private const int MaxAttempts = 10;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(3);

    public static void Run(string connectionString, ILogger logger)
    {
        WaitForDatabase(connectionString, logger);

        var upgrader = DeployChanges.To
            .MySqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(
                typeof(MigrationRunner).Assembly,
                name => name.Contains(".Migrations.", StringComparison.Ordinal))
            .WithTransactionPerScript()
            .LogToNowhere()
            .Build();

        var pending = upgrader.GetScriptsToExecute();
        if (pending.Count == 0)
        {
            logger.LogInformation("Base de datos al día; sin migraciones pendientes.");
            return;
        }

        logger.LogInformation("Aplicando {Count} migración(es): {Scripts}",
            pending.Count, string.Join(", ", pending.Select(s => s.Name)));

        var result = upgrader.PerformUpgrade();
        if (!result.Successful)
        {
            throw new InvalidOperationException(
                $"Falló la migración '{result.ErrorScript?.Name}'.", result.Error);
        }
    }

    // El contenedor de MySQL puede tardar unos segundos en aceptar conexiones tras `docker compose up`.
    private static void WaitForDatabase(string connectionString, ILogger logger)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                using var connection = new MySqlConnection(connectionString);
                connection.Open();
                return;
            }
            catch (MySqlException ex) when (attempt < MaxAttempts)
            {
                logger.LogWarning("Base de datos no disponible (intento {Attempt}/{Max}): {Message}",
                    attempt, MaxAttempts, ex.Message);
                Thread.Sleep(RetryDelay);
            }
        }
    }
}
