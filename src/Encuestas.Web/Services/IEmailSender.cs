using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Encuestas.Web.Services;

/// <summary>
/// Transporte de los correos transaccionales. Los controladores no lo usan directamente: encolan
/// en <see cref="Infrastructure.EmailOutbox"/> y el despachador en segundo plano llama a este emisor.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string recipient, string subject, string htmlBody);
}

/// <summary>Configuración del servidor SMTP (sección <c>Smtp</c> o variables <c>Smtp__*</c>).</summary>
public class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string? Host { get; set; }
    public int Port { get; set; } = 587;

    /// <summary>Usa STARTTLS. Desactívalo solo para servidores locales como Mailpit.</summary>
    public bool UseStartTls { get; set; } = true;

    public string? User { get; set; }
    public string? Password { get; set; }
    public string From { get; set; } = "Encuestas <no-reply@encuestas.local>";
}

/// <summary>Envía los correos por SMTP con MailKit (en local, a Mailpit).</summary>
public class SmtpEmailSender(SmtpOptions options) : IEmailSender
{
    public async Task SendAsync(string recipient, string subject, string htmlBody)
    {
        using var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(options.From));
        message.To.Add(MailboxAddress.Parse(recipient));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        // Tiempo máximo por operación SMTP (MailKit espera 2 minutos por defecto).
        using var client = new SmtpClient { Timeout = 10_000 };
        await client.ConnectAsync(options.Host ?? throw new InvalidOperationException("Falta Smtp:Host."), options.Port,
            options.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
        if (!string.IsNullOrEmpty(options.User))
        {
            await client.AuthenticateAsync(options.User, options.Password ?? string.Empty);
        }
        // MailKit 5 envía a través de una solicitud de envío en lugar de recibir el mensaje directamente.
        await client.SendAsync(new SmtpSendRequest(message));
        await client.DisconnectAsync(quit: true);
    }
}
