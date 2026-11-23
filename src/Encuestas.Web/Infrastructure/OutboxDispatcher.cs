using Encuestas.Web.Services;

namespace Encuestas.Web.Infrastructure;

/// <summary>
/// Envía en segundo plano los correos de <see cref="EmailOutbox"/>: despierta al encolarse un
/// mensaje o, como mínimo, cada 15 segundos para recoger los reintentos programados.
/// </summary>
public sealed class OutboxDispatcher(EmailOutbox outbox, IEmailSender sender, ILogger<OutboxDispatcher> logger)
    : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan ErrorDelay = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                while (await outbox.DispatchBatchAsync(sender, logger, stoppingToken) > 0)
                {
                }
                await outbox.WaitForMessagesAsync(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                // P. ej. la base de datos no responde: se reintenta más tarde sin tumbar la app.
                logger.LogError(ex, "Error al procesar la bandeja de salida de correos");
                await Task.Delay(ErrorDelay, stoppingToken);
            }
        }
    }
}
