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

    public async Task<bool> CreateUserAsync(string email, string passwordHash)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "INSERT INTO a_users(u_email, u_password) VALUES(@email, @password)";
            command.Parameters.AddWithValue("@email", email);
            command.Parameters.AddWithValue("@password", passwordHash);
            await command.ExecuteNonQueryAsync();
            var userId = command.LastInsertedId;

            command.Parameters.Clear();
            command.CommandText = "INSERT INTO a_users_profiles(u_p_key, u_p_user_name, u_p_name) VALUES(@id, @userName, @name)";
            command.Parameters.AddWithValue("@id", userId);
            command.Parameters.AddWithValue("@userName", "usuario" + userId);
            command.Parameters.AddWithValue("@name", "Usuario" + userId);
            await command.ExecuteNonQueryAsync();

            await transaction.CommitAsync();
            return true;
        }
        catch (MySqlException ex) when (ex.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
        {
            await transaction.RollbackAsync();
            return false;
        }
    }

    public async Task<User?> GetUserAsync(int id)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT u_key, u_email, u_password FROM a_users WHERE u_key=@id";
        command.Parameters.AddWithValue("@id", id);
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }
        return new User
        {
            Id = reader.GetInt32("u_key"),
            Email = reader.GetString("u_email"),
            PasswordHash = reader.GetString("u_password")
        };
    }

    public async Task<UserProfile?> GetProfileByEmailAsync(string email)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT u.u_key, u.u_email, u.u_password, up.u_p_user_name, up.u_p_name " +
                              "FROM a_users AS u JOIN a_users_profiles AS up ON u.u_key = up.u_p_key WHERE u.u_email=@email";
        command.Parameters.AddWithValue("@email", email);
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapProfile(reader) : null;
    }

    public async Task<UserProfile?> GetProfileByUserNameAsync(string userName)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT u.u_key, u.u_email, u.u_password, up.u_p_user_name, up.u_p_name " +
                              "FROM a_users AS u JOIN a_users_profiles AS up ON u.u_key = up.u_p_key WHERE up.u_p_user_name=@userName";
        command.Parameters.AddWithValue("@userName", userName);
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapProfile(reader) : null;
    }

    public Task<bool> UpdateNameAsync(int id, string name) =>
        ExecuteUpdateAsync("UPDATE a_users_profiles SET u_p_name=@value WHERE u_p_key=@id", id, name);

    public Task<bool> UpdateUserNameAsync(int id, string userName) =>
        ExecuteUpdateAsync("UPDATE a_users_profiles SET u_p_user_name=@value WHERE u_p_key=@id", id, userName);

    public Task<bool> UpdateEmailAsync(int id, string email) =>
        ExecuteUpdateAsync("UPDATE a_users SET u_email=@value WHERE u_key=@id", id, email);

    public Task<bool> UpdatePasswordAsync(int id, string passwordHash) =>
        ExecuteUpdateAsync("UPDATE a_users SET u_password=@value WHERE u_key=@id", id, passwordHash);

    public async Task<bool> DeleteAccountAsync(int id)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM a_users WHERE u_key=@id";
        command.Parameters.AddWithValue("@id", id);
        return await command.ExecuteNonQueryAsync() == 1;
    }

    private async Task<bool> ExecuteUpdateAsync(string sql, int id, string value)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("@id", id);
        command.Parameters.AddWithValue("@value", value);
        try
        {
            return await command.ExecuteNonQueryAsync() == 1;
        }
        catch (MySqlException ex) when (ex.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
        {
            return false;
        }
    }

    private static UserProfile MapProfile(MySqlDataReader reader) => new()
    {
        User = new User
        {
            Id = reader.GetInt32("u_key"),
            Email = reader.GetString("u_email"),
            PasswordHash = reader.GetString("u_password")
        },
        UserName = reader.GetString("u_p_user_name"),
        Name = reader.GetString("u_p_name")
    };
}
