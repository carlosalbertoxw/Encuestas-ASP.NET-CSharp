namespace Encuestas.Web.Services;

/// <summary>Envío de correos transaccionales (confirmación de cuenta, restablecimiento).</summary>
public interface IEmailSender
{
    Task SendAsync(string recipient, string subject, string htmlBody);
}

/// <summary>
/// Implementación por defecto que registra el correo en el log en lugar de enviarlo. Sirve para
/// desarrollo y pruebas; en producción debe sustituirse por un proveedor real (SMTP, SendGrid…).
/// </summary>
public class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string recipient, string subject, string htmlBody)
    {
        _logger.LogInformation("[Correo simulado] Para: {To} | Asunto: {Subject}\n{Body}", recipient, subject, htmlBody);
        return Task.CompletedTask;
    }
}
