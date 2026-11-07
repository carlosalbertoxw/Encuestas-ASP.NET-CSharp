using Dapper;
using Encuestas.Model;
using MySqlConnector;

namespace Encuestas.Data;

public class UserRepository(MySqlDataSource dataSource) : IUserRepository
{
    private const string UserColumns =
        "u.u_key AS Id, u.u_email AS Email, u.u_email_confirmed AS EmailConfirmed, u.u_pending_email AS PendingEmail, " +
        "u.u_password AS PasswordHash, u.u_security_stamp AS SecurityStamp";

    // Dapper divide cada fila en User y UserProfile a partir de la columna UserName (splitOn).
    private const string ProfileQuery =
        "SELECT " + UserColumns + ", up.u_p_user_name AS UserName, up.u_p_name AS Name " +
        "FROM a_users AS u JOIN a_users_profiles AS up ON u.u_key = up.u_p_key ";

    public async Task<RepositoryResult> CreateUserAsync(string email, string passwordHash, string securityStamp)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            var userId = await connection.ExecuteScalarAsync<long>(
                "INSERT INTO a_users(u_email, u_password, u_security_stamp) VALUES(@email, @passwordHash, @securityStamp); " +
                "SELECT LAST_INSERT_ID();",
                new { email, passwordHash, securityStamp }, transaction);

            await connection.ExecuteAsync(
                "INSERT INTO a_users_profiles(u_p_key, u_p_user_name, u_p_name) VALUES(@userId, @userName, @name)",
                new { userId, userName = "usuario" + userId, name = "Usuario" + userId }, transaction);

            await transaction.CommitAsync();
            return RepositoryResult.Success;
        }
        catch (MySqlException ex) when (ex.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
        {
            await transaction.RollbackAsync();
            return RepositoryResult.Duplicate;
        }
    }

    public async Task<User?> GetUserAsync(int id)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<User>(
            "SELECT " + UserColumns + " FROM a_users AS u WHERE u.u_key=@id", new { id });
    }

    public Task<UserProfile?> GetProfileByIdAsync(int id) =>
        QueryProfileAsync("WHERE u.u_key=@value", id);

    public Task<UserProfile?> GetProfileByEmailAsync(string email) =>
        QueryProfileAsync("WHERE u.u_email=@value", email);

    public Task<UserProfile?> GetProfileByUserNameAsync(string userName) =>
        QueryProfileAsync("WHERE up.u_p_user_name=@value", userName);

    public async Task<string?> GetSecurityStampAsync(int id)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.ExecuteScalarAsync<string?>(
            "SELECT u_security_stamp FROM a_users WHERE u_key=@id", new { id });
    }

    public Task<RepositoryResult> ConfirmEmailAsync(int id) =>
        ExecuteAsync("UPDATE a_users SET u_email_confirmed=1 WHERE u_key=@id", new { id });

    public Task<RepositoryResult> UpdateNameAsync(int id, string name) =>
        ExecuteAsync("UPDATE a_users_profiles SET u_p_name=@name WHERE u_p_key=@id", new { id, name });

    public Task<RepositoryResult> UpdateUserNameAsync(int id, string userName) =>
        ExecuteAsync("UPDATE a_users_profiles SET u_p_user_name=@userName WHERE u_p_key=@id", new { id, userName });

    public Task<RepositoryResult> SetPendingEmailAsync(int id, string email) =>
        ExecuteAsync("UPDATE a_users SET u_pending_email=@email WHERE u_key=@id", new { id, email });

    public Task<RepositoryResult> ConfirmEmailChangeAsync(int id, string email, string newSecurityStamp) =>
        ExecuteAsync(
            "UPDATE a_users SET u_email=u_pending_email, u_pending_email=NULL, u_email_confirmed=1, u_security_stamp=@newSecurityStamp " +
            "WHERE u_key=@id AND u_pending_email=@email",
            new { id, email, newSecurityStamp });

    public Task<RepositoryResult> UpdatePasswordAsync(int id, string passwordHash) =>
        ExecuteAsync("UPDATE a_users SET u_password=@passwordHash WHERE u_key=@id", new { id, passwordHash });

    public Task<RepositoryResult> ChangePasswordAsync(int id, string passwordHash, string newSecurityStamp) =>
        ExecuteAsync("UPDATE a_users SET u_password=@passwordHash, u_security_stamp=@newSecurityStamp WHERE u_key=@id",
            new { id, passwordHash, newSecurityStamp });

    public Task<RepositoryResult> DeleteAccountAsync(int id) =>
        ExecuteAsync("DELETE FROM a_users WHERE u_key=@id", new { id });

    private async Task<UserProfile?> QueryProfileAsync(string where, object value)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        var rows = await connection.QueryAsync<User, UserProfile, UserProfile>(
            ProfileQuery + where,
            (user, profile) =>
            {
                profile.User = user;
                return profile;
            },
            new { value },
            splitOn: "UserName");
        return rows.SingleOrDefault();
    }

    private async Task<RepositoryResult> ExecuteAsync(string sql, object parameters)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        try
        {
            return await connection.ExecuteAsync(sql, parameters) == 1 ? RepositoryResult.Success : RepositoryResult.NotFound;
        }
        catch (MySqlException ex) when (ex.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
        {
            return RepositoryResult.Duplicate;
        }
    }
}
