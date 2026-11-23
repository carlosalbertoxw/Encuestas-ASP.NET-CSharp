using Dapper;
using Encuestas.Web.Services;
using MySqlConnector;

namespace Encuestas.Web.Infrastructure;

/// <summary>
/// Bandeja de salida de correos (tabla <c>a_outbox</c>). Las peticiones solo encolan, así que
/// nunca esperan al SMTP ni fallan si está caído; <see cref="OutboxDispatcher"/> envía en segundo
/// plano con reintentos y espera exponencial. Los mensajes enviados se borran (llevan tokens).
/// </summary>
public sealed class EmailOutbox(MySqlDataSource dataSource) : IDisposable
{
    public const int MaxAttempts = 8;
    private const int BatchSize = 10;
    private static readonly TimeSpan BaseDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan MaxDelay = TimeSpan.FromHours(1);

    // Despierta al despachador en cuanto hay un mensaje nuevo, sin esperar al siguiente sondeo.
    private readonly SemaphoreSlim _signal = new(0);

    public async Task EnqueueAsync(string recipient, string subject, string htmlBody)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        await connection.ExecuteAsync(
            "INSERT INTO a_outbox(o_recipient, o_subject, o_body, o_next_attempt_at) " +
            "VALUES(@recipient, @subject, @htmlBody, UTC_TIMESTAMP())",
            new { recipient, subject, htmlBody });
        _signal.Release();
    }

    /// <summary>Espera hasta que se encole un mensaje o pase <paramref name="timeout"/>.</summary>
    public Task WaitForMessagesAsync(TimeSpan timeout, CancellationToken cancellationToken) =>
        _signal.WaitAsync(timeout, cancellationToken);

    /// <summary>
    /// Envía un lote de mensajes pendientes. Las filas se bloquean con <c>SKIP LOCKED</c>, así que
    /// varias réplicas pueden despachar a la vez sin enviar dos veces el mismo correo.
    /// </summary>
    /// <returns>Número de mensajes procesados (enviados o reprogramados).</returns>
    public async Task<int> DispatchBatchAsync(IEmailSender sender, ILogger logger, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var messages = (await connection.QueryAsync<OutboxMessage>(
            "SELECT o_key AS Id, o_recipient AS Recipient, o_subject AS Subject, o_body AS Body, o_attempts AS Attempts " +
            "FROM a_outbox WHERE o_failed_at IS NULL AND o_next_attempt_at <= UTC_TIMESTAMP() " +
            "ORDER BY o_key LIMIT @BatchSize FOR UPDATE SKIP LOCKED",
            new { BatchSize }, transaction)).AsList();

        foreach (var message in messages)
        {
            try
            {
                await sender.SendAsync(message.Recipient, message.Subject, message.Body);
                await connection.ExecuteAsync("DELETE FROM a_outbox WHERE o_key=@Id", new { message.Id }, transaction);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                var attempts = message.Attempts + 1;
                var failed = attempts >= MaxAttempts;
                // El destinatario no se registra: es un dato personal.
                logger.LogWarning(ex, "Fallo al enviar el correo {OutboxId} (intento {Attempt}/{Max})",
                    message.Id, attempts, MaxAttempts);
                await connection.ExecuteAsync(
                    "UPDATE a_outbox SET o_attempts=@attempts, o_last_error=@error, " +
                    "o_next_attempt_at=UTC_TIMESTAMP() + INTERVAL @delay SECOND, " +
                    "o_failed_at=IF(@failed, UTC_TIMESTAMP(), NULL) WHERE o_key=@Id",
                    new { message.Id, attempts, failed, error = Truncate(ex.Message, 200), delay = (int)Backoff(attempts).TotalSeconds },
                    transaction);
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return messages.Count;
    }

    /// <summary>Espera exponencial: 30 s, 1 min, 2 min… hasta 1 h.</summary>
    public static TimeSpan Backoff(int attempts) =>
        TimeSpan.FromSeconds(Math.Min(BaseDelay.TotalSeconds * Math.Pow(2, attempts - 1), MaxDelay.TotalSeconds));

    private static string Truncate(string value, int length) => value.Length <= length ? value : value[..length];

    public void Dispose() => _signal.Dispose();

    private sealed record OutboxMessage(long Id, string Recipient, string Subject, string Body, int Attempts);
}
