using Encuestas.Web.Services;

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
}
