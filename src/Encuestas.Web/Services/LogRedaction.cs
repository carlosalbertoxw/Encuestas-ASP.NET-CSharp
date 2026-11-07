namespace Encuestas.Web.Services;

/// <summary>Enmascara datos personales antes de escribirlos en los logs.</summary>
public static class LogRedaction
{
    /// <summary><c>demo@encuestas.dev</c> → <c>d***@encuestas.dev</c>. Basta para correlacionar intentos sin exponer el correo.</summary>
    public static string Email(string? email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return string.Empty;
        }
        var at = email.IndexOf('@', StringComparison.Ordinal);
        return at <= 0 ? "***" : string.Concat(email.AsSpan(0, 1), "***", email.AsSpan(at));
    }
}
