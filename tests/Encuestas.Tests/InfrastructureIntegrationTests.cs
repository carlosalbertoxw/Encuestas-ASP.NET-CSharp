using Dapper;
using Encuestas.Web.Infrastructure;
using Encuestas.Web.Services;
using Microsoft.Extensions.Logging.Abstractions;
using MySqlConnector;

namespace Encuestas.Tests;

/// <summary>Contadores de intentos y bandeja de salida de correos contra un MySQL real.</summary>
[Collection("mysql")]
[Trait("Category", "Integration")]
public sealed class InfrastructureIntegrationTests(MySqlContainerFixture fixture) : IDisposable
{
    private readonly MySqlDataSource _dataSource = fixture.DataSource;
    private readonly EmailOutbox _outbox = new(fixture.DataSource);

    public void Dispose() => _outbox.Dispose();

    [Fact]
    public async Task Contador_incrementa_cuenta_y_se_reinicia()
    {
        var store = new MySqlAttemptCounterStore(_dataSource);
        var window = TimeSpan.FromMinutes(1);

        await store.IncrementAsync("prueba:contador", window);
        await store.IncrementAsync("prueba:contador", window);
        Assert.Equal(2, await store.CountAsync("prueba:contador", window));

        await store.ResetAsync("prueba:contador");
        Assert.Equal(0, await store.CountAsync("prueba:contador", window));
    }

    [Fact]
    public async Task Contador_empieza_una_ventana_nueva_cuando_la_anterior_caduca()
    {
        var store = new MySqlAttemptCounterStore(_dataSource);
        await store.IncrementAsync("prueba:ventana", TimeSpan.FromMinutes(1));
        await using (var connection = await _dataSource.OpenConnectionAsync(TestContext.Current.CancellationToken))
        {
            await connection.ExecuteAsync(
                "UPDATE a_attempt_counters SET ac_window_start = UTC_TIMESTAMP() - INTERVAL 2 MINUTE WHERE ac_key='prueba:ventana'");
        }

        Assert.Equal(0, await store.CountAsync("prueba:ventana", TimeSpan.FromMinutes(1)));
        await store.IncrementAsync("prueba:ventana", TimeSpan.FromMinutes(1));
        Assert.Equal(1, await store.CountAsync("prueba:ventana", TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public async Task Outbox_envia_y_borra_los_mensajes()
    {
        var sender = new CapturingEmailSender();
        await _outbox.EnqueueAsync("outbox-ok@test.com", "Asunto", "<p>Hola</p>");

        await DispatchAllAsync(sender);

        Assert.Contains(sender.Sent, m => m.To == "outbox-ok@test.com" && m.Subject == "Asunto");
        Assert.Equal(0, await CountRowsAsync("outbox-ok@test.com"));
    }

    [Fact]
    public async Task Outbox_reprograma_el_mensaje_si_el_envio_falla()
    {
        await _outbox.EnqueueAsync("outbox-falla@test.com", "Asunto", "<p>Hola</p>");

        await DispatchAllAsync(new FailingEmailSender());

        await using var connection = await _dataSource.OpenConnectionAsync(TestContext.Current.CancellationToken);
        var row = await connection.QuerySingleAsync<(int Attempts, bool Scheduled)>(
            "SELECT o_attempts, o_next_attempt_at > UTC_TIMESTAMP() FROM a_outbox WHERE o_recipient='outbox-falla@test.com'");
        Assert.Equal(1, row.Attempts);
        Assert.True(row.Scheduled);
    }

    [Theory]
    [InlineData(1, 30)]
    [InlineData(2, 60)]
    [InlineData(5, 480)]
    [InlineData(20, 3600)]
    public void La_espera_entre_reintentos_crece_hasta_una_hora(int attempts, int expectedSeconds)
    {
        Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), EmailOutbox.Backoff(attempts));
    }

    private async Task DispatchAllAsync(IEmailSender sender)
    {
        while (await _outbox.DispatchBatchAsync(sender, NullLogger.Instance, TestContext.Current.CancellationToken) > 0)
        {
        }
    }

    private async Task<int> CountRowsAsync(string recipient)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(TestContext.Current.CancellationToken);
        return await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM a_outbox WHERE o_recipient=@recipient", new { recipient });
    }

    private sealed class FailingEmailSender : IEmailSender
    {
        public Task SendAsync(string recipient, string subject, string htmlBody) =>
            throw new InvalidOperationException("SMTP no disponible");
    }
}
