using Encuestas.Web.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Encuestas.Tests;

public class SecurityStampCacheTests
{
    private static SecurityStampCache Create() => new(new MemoryCache(new MemoryCacheOptions()));

    [Fact]
    public async Task GetAsync_cachea_el_sello_y_solo_consulta_una_vez()
    {
        var cache = Create();
        var llamadas = 0;
        Task<string?> Fetch()
        {
            llamadas++;
            return Task.FromResult<string?>("sello-1");
        }

        var primera = await cache.GetAsync(1, Fetch);
        var segunda = await cache.GetAsync(1, Fetch);

        Assert.Equal("sello-1", primera);
        Assert.Equal("sello-1", segunda);
        Assert.Equal(1, llamadas);
    }

    [Fact]
    public async Task Invalidate_fuerza_a_reconsultar_en_la_siguiente_llamada()
    {
        var cache = Create();
        var llamadas = 0;
        Task<string?> Fetch()
        {
            llamadas++;
            return Task.FromResult<string?>($"sello-{llamadas}");
        }

        var antes = await cache.GetAsync(1, Fetch);
        cache.Invalidate(1);
        var despues = await cache.GetAsync(1, Fetch);

        Assert.Equal("sello-1", antes);
        Assert.Equal("sello-2", despues);
        Assert.Equal(2, llamadas);
    }

    [Fact]
    public async Task Usuarios_distintos_usan_entradas_independientes()
    {
        var cache = Create();

        var deUno = await cache.GetAsync(1, () => Task.FromResult<string?>("sello-a"));
        var deDos = await cache.GetAsync(2, () => Task.FromResult<string?>("sello-b"));

        Assert.Equal("sello-a", deUno);
        Assert.Equal("sello-b", deDos);
    }
}
