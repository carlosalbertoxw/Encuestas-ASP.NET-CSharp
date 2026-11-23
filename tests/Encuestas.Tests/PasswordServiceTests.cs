using Encuestas.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Encuestas.Tests;

public class PasswordServiceTests
{
    private readonly PasswordService _service = new();

    [Fact]
    public void Hash_y_Verify_aceptan_la_contraseña_correcta()
    {
        var hash = _service.Hash("Secreta123");

        Assert.Equal(PasswordVerificationOutcome.Success, _service.Verify(hash, "Secreta123"));
    }

    [Fact]
    public void Verify_rechaza_una_contraseña_incorrecta()
    {
        var hash = _service.Hash("Secreta123");

        Assert.Equal(PasswordVerificationOutcome.Failed, _service.Verify(hash, "otra"));
    }

    [Fact]
    public void Hash_genera_salt_distinto_en_cada_llamada()
    {
        Assert.NotEqual(_service.Hash("Secreta123"), _service.Hash("Secreta123"));
    }

    [Fact]
    public void Verify_trata_un_hash_corrupto_como_credencial_invalida()
    {
        Assert.Equal(PasswordVerificationOutcome.Failed,
            _service.Verify("esto-no-es-un-hash-válido", "cualquiera"));
    }

    [Fact]
    public void Verify_señala_rehash_para_un_hash_legado_Identity_V2()
    {
        var legacyHasher = new PasswordHasher<object>(Options.Create(
            new PasswordHasherOptions { CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV2 }));
        var legacyHash = legacyHasher.HashPassword(new object(), "Secreta123");

        Assert.Equal(PasswordVerificationOutcome.SuccessRehashNeeded,
            _service.Verify(legacyHash, "Secreta123"));
    }

    [Fact]
    public void Verify_señala_rehash_para_un_hash_con_menos_iteraciones()
    {
        // Mismo formato (Identity V3), pero con las 100 000 iteraciones por defecto de Identity.
        var weakerHash = new PasswordHasher<object>().HashPassword(new object(), "Secreta123");

        Assert.Equal(PasswordVerificationOutcome.SuccessRehashNeeded, _service.Verify(weakerHash, "Secreta123"));
        Assert.Equal(PasswordVerificationOutcome.Success, _service.Verify(_service.Hash("Secreta123"), "Secreta123"));
    }

    [Fact]
    public void VerifyAgainstDummy_no_falla_con_cualquier_contraseña()
    {
        _service.VerifyAgainstDummy("loquesea");
        _service.VerifyAgainstDummy(string.Empty);
    }
}
