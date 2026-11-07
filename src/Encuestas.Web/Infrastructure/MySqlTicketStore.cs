using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using Dapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using MySqlConnector;

namespace Encuestas.Web.Infrastructure;

/// <summary>
/// Guarda las sesiones de autenticación en la tabla <c>a_sessions</c>: la cookie solo lleva un
/// identificador aleatorio. Cerrar sesión o borrar la cuenta elimina la fila, así que una copia
/// de la cookie deja de servir de inmediato (con la cookie autocontenida seguía siendo válida
/// hasta caducar).
/// </summary>
public class MySqlTicketStore(MySqlDataSource dataSource) : ITicketStore
{
    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var key = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var userId = int.Parse(ticket.Principal.FindFirstValue(ClaimTypes.NameIdentifier)!, CultureInfo.InvariantCulture);

        await using var connection = await dataSource.OpenConnectionAsync();
        // Limpieza oportunista de sesiones caducadas, sin necesidad de un proceso aparte.
        await connection.ExecuteAsync(
            "DELETE FROM a_sessions WHERE s_expires_at < UTC_TIMESTAMP() LIMIT 100");
        await connection.ExecuteAsync(
            "INSERT INTO a_sessions(s_key, s_user_key, s_ticket, s_expires_at) VALUES(@key, @userId, @ticket, @expires)",
            new { key, userId, ticket = Serialize(ticket), expires = Expiration(ticket) });
        return key;
    }

    public async Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        await connection.ExecuteAsync(
            "UPDATE a_sessions SET s_ticket=@ticket, s_expires_at=@expires WHERE s_key=@key",
            new { key, ticket = Serialize(ticket), expires = Expiration(ticket) });
    }

    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        var data = await connection.QuerySingleOrDefaultAsync<byte[]>(
            "SELECT s_ticket FROM a_sessions WHERE s_key=@key AND (s_expires_at IS NULL OR s_expires_at > UTC_TIMESTAMP())",
            new { key });
        return data is null ? null : TicketSerializer.Default.Deserialize(data);
    }

    public async Task RemoveAsync(string key)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        await connection.ExecuteAsync("DELETE FROM a_sessions WHERE s_key=@key", new { key });
    }

    private static byte[] Serialize(AuthenticationTicket ticket) => TicketSerializer.Default.Serialize(ticket);

    private static DateTime? Expiration(AuthenticationTicket ticket) => ticket.Properties.ExpiresUtc?.UtcDateTime;
}

/// <summary>Conecta el <see cref="ITicketStore"/> registrado con las opciones de la cookie.</summary>
public static class TicketStoreExtensions
{
    public static IServiceCollection AddMySqlTicketStore(this IServiceCollection services)
    {
        services.AddSingleton<ITicketStore, MySqlTicketStore>();
        services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
            .Configure<ITicketStore>((options, store) => options.SessionStore = store);
        return services;
    }
}
