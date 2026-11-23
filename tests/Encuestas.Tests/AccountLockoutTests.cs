using Encuestas.Web.Services;

namespace Encuestas.Tests;

public class AccountLockoutTests
{
    private readonly InMemoryAttemptCounterStore _store = new();

    private AccountLockout Create() => new(_store);

    private static async Task FailAsync(AccountLockout lockout, string email, int times)
    {
        for (var i = 0; i < times; i++)
        {
            await lockout.RecordFailureAsync(email);
        }
    }

    [Fact]
    public async Task No_bloquea_por_debajo_del_umbral()
    {
        var lockout = Create();
        await FailAsync(lockout, "a@b.c", AccountLockout.MaxAttempts - 1);
        Assert.False(await lockout.IsLockedAsync("a@b.c"));
    }

    [Fact]
    public async Task Bloquea_al_alcanzar_el_umbral()
    {
        var lockout = Create();
        await FailAsync(lockout, "a@b.c", AccountLockout.MaxAttempts);
        Assert.True(await lockout.IsLockedAsync("a@b.c"));
    }

    [Fact]
    public async Task Reset_limpia_el_contador()
    {
        var lockout = Create();
        await FailAsync(lockout, "a@b.c", AccountLockout.MaxAttempts);
        await lockout.ResetAsync("a@b.c");
        Assert.False(await lockout.IsLockedAsync("a@b.c"));
    }

    [Fact]
    public async Task Es_insensible_a_mayusculas_en_el_correo()
    {
        var lockout = Create();
        await FailAsync(lockout, "Correo@Test.COM", AccountLockout.MaxAttempts);
        Assert.True(await lockout.IsLockedAsync("correo@test.com"));
    }

    [Fact]
    public async Task El_bloqueo_caduca_al_terminar_la_ventana()
    {
        var lockout = Create();
        await FailAsync(lockout, "a@b.c", AccountLockout.MaxAttempts);
        _store.Now += AccountLockout.Window + TimeSpan.FromSeconds(1);
        Assert.False(await lockout.IsLockedAsync("a@b.c"));
    }
}
