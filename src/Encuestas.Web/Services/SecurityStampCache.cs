using Microsoft.Extensions.Caching.Memory;

namespace Encuestas.Web.Services;

/// <summary>
/// Cachea el sello de seguridad de cada usuario con un TTL corto para no consultar la BD en
/// cada request. Al cambiar la contraseña se invalida la entrada para que la sesión
/// actual (ya reemitida con el sello nuevo) siga siendo válida de inmediato.
/// </summary>
public class SecurityStampCache(IMemoryCache cache)
{
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(30);
    public async Task<string?> GetAsync(int userId, Func<Task<string?>> fetch)
    {
        return await cache.GetOrCreateAsync(Key(userId), entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = Ttl;
            return fetch();
        });
    }

    public void Invalidate(int userId) => cache.Remove(Key(userId));

    private static string Key(int userId) => $"secstamp:{userId}";
}
