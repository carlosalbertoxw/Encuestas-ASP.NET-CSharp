using Encuestas.Web.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Encuestas.Tests;

public class AccountLockoutTests
{
    private static AccountLockout Create() => new(new MemoryCache(new MemoryCacheOptions()));

    [Fact]
    public void No_bloquea_por_debajo_del_umbral()
    {
        var lockout = Create();
        for (var i = 0; i < 4; i++)
        {
            lockout.RecordFailure("a@b.c");
        }
        Assert.False(lockout.IsLocked("a@b.c"));
    }

    [Fact]
    public void Bloquea_al_alcanzar_el_umbral()
    {
        var lockout = Create();
        for (var i = 0; i < 5; i++)
        {
            lockout.RecordFailure("a@b.c");
        }
        Assert.True(lockout.IsLocked("a@b.c"));
    }

    [Fact]
    public void Reset_limpia_el_contador()
    {
        var lockout = Create();
        for (var i = 0; i < 5; i++)
        {
            lockout.RecordFailure("a@b.c");
        }
        lockout.Reset("a@b.c");
        Assert.False(lockout.IsLocked("a@b.c"));
    }

    [Fact]
    public void Es_insensible_a_mayusculas_en_el_correo()
    {
        var lockout = Create();
        for (var i = 0; i < 5; i++)
        {
            lockout.RecordFailure("Correo@Test.COM");
        }
        Assert.True(lockout.IsLocked("correo@test.com"));
    }
}
