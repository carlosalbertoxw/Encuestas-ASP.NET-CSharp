using Encuestas.Model;
using MySqlConnector;

namespace Encuestas.Data;

public class UserRepository : IUserRepository
{
    private readonly MySqlDataSource _dataSource;

    public UserRepository(MySqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<RepositoryResult> CreateUserAsync(string email, string passwordHash, string securityStamp)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO a_users(u_email, u_password, u_security_stamp) VALUES(@email, @password, @stamp)";
            command.Parameters.AddWithValue("@email", email);
            command.Parameters.AddWithValue("@password", passwordHash);
            command.Parameters.AddWithValue("@stamp", securityStamp);
            await command.ExecuteNonQueryAsync();
            var userId = command.LastInsertedId;

            command.Parameters.Clear();
            command.CommandText = "INSERT INTO a_users_profiles(u_p_key, u_p_user_name, u_p_name) VALUES(@id, @userName, @name)";
            command.Parameters.AddWithValue("@id", userId);
            command.Parameters.AddWithValue("@userName", "usuario" + userId);
            command.Parameters.AddWithValue("@name", "Usuario" + userId);
            await command.ExecuteNonQueryAsync();

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
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT u_key, u_email, u_email_confirmed, u_password, u_security_stamp FROM a_users WHERE u_key=@id";
        command.Parameters.AddWithValue("@id", id);
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapUser(reader) : null;
    }

    public async Task<string?> GetSecurityStampAsync(int id)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT u_security_stamp FROM a_users WHERE u_key=@id";
        command.Parameters.AddWithValue("@id", id);
        return await command.ExecuteScalarAsync() as string;
    }

    public Task<RepositoryResult> ConfirmEmailAsync(int id) =>
        ExecuteUpdateAsync("UPDATE a_users SET u_email_confirmed=1 WHERE u_key=@id", id);

    public async Task<UserProfile?> GetProfileByEmailAsync(string email)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT u.u_key, u.u_email, u.u_email_confirmed, u.u_password, u.u_security_stamp, up.u_p_user_name, up.u_p_name " +
                              "FROM a_users AS u JOIN a_users_profiles AS up ON u.u_key = up.u_p_key WHERE u.u_email=@email";
        command.Parameters.AddWithValue("@email", email);
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapProfile(reader) : null;
    }

    public async Task<UserProfile?> GetProfileByUserNameAsync(string userName)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT u.u_key, u.u_email, u.u_email_confirmed, u.u_password, u.u_security_stamp, up.u_p_user_name, up.u_p_name " +
                              "FROM a_users AS u JOIN a_users_profiles AS up ON u.u_key = up.u_p_key WHERE up.u_p_user_name=@userName";
        command.Parameters.AddWithValue("@userName", userName);
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapProfile(reader) : null;
    }

    public Task<RepositoryResult> UpdateNameAsync(int id, string name) =>
        ExecuteUpdateAsync("UPDATE a_users_profiles SET u_p_name=@value WHERE u_p_key=@id", id, ("@value", name));

    public Task<RepositoryResult> UpdateUserNameAsync(int id, string userName) =>
        ExecuteUpdateAsync("UPDATE a_users_profiles SET u_p_user_name=@value WHERE u_p_key=@id", id, ("@value", userName));

    public Task<RepositoryResult> UpdateEmailAsync(int id, string email) =>
        ExecuteUpdateAsync("UPDATE a_users SET u_email=@value WHERE u_key=@id", id, ("@value", email));

    public Task<RepositoryResult> UpdatePasswordAsync(int id, string passwordHash) =>
        ExecuteUpdateAsync("UPDATE a_users SET u_password=@value WHERE u_key=@id", id, ("@value", passwordHash));

    public Task<RepositoryResult> ChangePasswordAsync(int id, string passwordHash, string newSecurityStamp) =>
        ExecuteUpdateAsync("UPDATE a_users SET u_password=@value, u_security_stamp=@stamp WHERE u_key=@id",
            id, ("@value", passwordHash), ("@stamp", newSecurityStamp));

    public async Task<RepositoryResult> DeleteAccountAsync(int id)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM a_users WHERE u_key=@id";
        command.Parameters.AddWithValue("@id", id);
        return await command.ExecuteNonQueryAsync() == 1 ? RepositoryResult.Success : RepositoryResult.NotFound;
    }

    private async Task<RepositoryResult> ExecuteUpdateAsync(string sql, int id, params (string Name, object Value)[] parameters)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@id", id);
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }
        try
        {
            return await command.ExecuteNonQueryAsync() == 1 ? RepositoryResult.Success : RepositoryResult.NotFound;
        }
        catch (MySqlException ex) when (ex.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
        {
            return RepositoryResult.Duplicate;
        }
    }

    private static User MapUser(MySqlDataReader reader) => new()
    {
        Id = reader.GetInt32("u_key"),
        Email = reader.GetString("u_email"),
        EmailConfirmed = reader.GetBoolean("u_email_confirmed"),
        PasswordHash = reader.GetString("u_password"),
        SecurityStamp = reader.GetString("u_security_stamp")
    };

    private static UserProfile MapProfile(MySqlDataReader reader) => new()
    {
        User = MapUser(reader),
        UserName = reader.GetString("u_p_user_name"),
        Name = reader.GetString("u_p_name")
    };
}
