using Microsoft.Extensions.Caching.Memory;

namespace Encuestas.Web.Services;

/// <summary>
/// Bloqueo por cuenta: tras varios intentos de inicio de sesión fallidos sobre el mismo correo
/// se rechazan nuevos intentos durante una ventana, incluso con la contraseña correcta (SEG-03).
/// Complementa al rate limiting por IP. El estado vive en memoria: con múltiples réplicas hace
/// falta un almacén compartido (p. ej. Redis).
/// </summary>
public class AccountLockout
{
    private const int MaxAttempts = 5;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(15);
    private readonly IMemoryCache _cache;

    public AccountLockout(IMemoryCache cache)
    {
        _cache = cache;
    }

    public bool IsLocked(string email) =>
        _cache.TryGetValue(Key(email), out int attempts) && attempts >= MaxAttempts;

    public void RecordFailure(string email)
    {
        var attempts = _cache.TryGetValue(Key(email), out int current) ? current : 0;
        _cache.Set(Key(email), attempts + 1, Window);
    }

    public void Reset(string email) => _cache.Remove(Key(email));

    private static string Key(string email) => $"lockout:{email.ToLowerInvariant()}";
}
