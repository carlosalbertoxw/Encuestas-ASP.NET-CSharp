using Dapper;
using MySqlConnector;

namespace Encuestas.Web.Infrastructure;

/// <summary>
/// Limpieza periódica de filas caducadas: sesiones vencidas, contadores de intentos fuera de
/// su ventana y correos que agotaron los reintentos hace más de una semana.
/// </summary>
public sealed class DatabaseCleanupService(MySqlDataSource dataSource, ILogger<DatabaseCleanupService> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    // La ventana más larga de los contadores es de una hora; un día deja margen de sobra.
    private const string CleanupSql =
        "DELETE FROM a_sessions WHERE s_expires_at < UTC_TIMESTAMP(); " +
        "DELETE FROM a_attempt_counters WHERE ac_window_start < UTC_TIMESTAMP() - INTERVAL 1 DAY; " +
        "DELETE FROM a_outbox WHERE o_failed_at < UTC_TIMESTAMP() - INTERVAL 7 DAY;";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                await using var connection = await dataSource.OpenConnectionAsync(stoppingToken);
                await connection.ExecuteAsync(new CommandDefinition(CleanupSql, cancellationToken: stoppingToken));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "No se pudo limpiar la base de datos; se reintentará en la próxima ejecución");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
