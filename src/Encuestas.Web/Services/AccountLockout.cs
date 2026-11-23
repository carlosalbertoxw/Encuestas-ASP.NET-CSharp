namespace Encuestas.Web.Services;

/// <summary>
/// Bloqueo por cuenta: tras varios intentos de inicio de sesión fallidos sobre el mismo correo
/// se rechazan nuevos intentos durante una ventana, incluso con la contraseña correcta.
/// Complementa al rate limiting por IP. Los contadores viven en MySQL, compartidos entre réplicas.
/// </summary>
public class AccountLockout(IAttemptCounterStore store)
{
    public const int MaxAttempts = 5;
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(15);

    public async Task<bool> IsLockedAsync(string email) =>
        await store.CountAsync(Key(email), Window) >= MaxAttempts;

    public Task RecordFailureAsync(string email) => store.IncrementAsync(Key(email), Window);

    public Task ResetAsync(string email) => store.ResetAsync(Key(email));

    private static string Key(string email) => $"lockout:{email.ToLowerInvariant()}";
}
