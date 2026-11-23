using Dapper;
using Encuestas.Web.Services;
using MySqlConnector;

namespace Encuestas.Web.Infrastructure;

/// <summary>
/// Contadores en la tabla <c>a_attempt_counters</c>. El incremento es una sola sentencia
/// atómica (INSERT … ON DUPLICATE KEY UPDATE), así que dos réplicas no pierden intentos.
/// <see cref="DatabaseCleanupService"/> borra las filas caducadas.
/// </summary>
public class MySqlAttemptCounterStore(MySqlDataSource dataSource) : IAttemptCounterStore
{
    public async Task<int> CountAsync(string key, TimeSpan window)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.ExecuteScalarAsync<int?>(
            "SELECT ac_count FROM a_attempt_counters " +
            "WHERE ac_key=@key AND ac_window_start > UTC_TIMESTAMP() - INTERVAL @seconds SECOND",
            new { key, seconds = Seconds(window) }) ?? 0;
    }

    public async Task IncrementAsync(string key, TimeSpan window)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        // MySQL evalúa las asignaciones en orden: ac_count se calcula con la ventana anterior.
        await connection.ExecuteAsync(
            "INSERT INTO a_attempt_counters(ac_key, ac_count, ac_window_start) VALUES(@key, 1, UTC_TIMESTAMP()) " +
            "ON DUPLICATE KEY UPDATE " +
            "ac_count = IF(ac_window_start > UTC_TIMESTAMP() - INTERVAL @seconds SECOND, ac_count + 1, 1), " +
            "ac_window_start = IF(ac_window_start > UTC_TIMESTAMP() - INTERVAL @seconds SECOND, ac_window_start, UTC_TIMESTAMP())",
            new { key, seconds = Seconds(window) });
    }

    public async Task ResetAsync(string key)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        await connection.ExecuteAsync("DELETE FROM a_attempt_counters WHERE ac_key=@key", new { key });
    }

    private static int Seconds(TimeSpan window) => (int)window.TotalSeconds;
}
