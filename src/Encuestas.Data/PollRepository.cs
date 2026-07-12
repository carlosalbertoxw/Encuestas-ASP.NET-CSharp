using Encuestas.Model;
using MySqlConnector;

namespace Encuestas.Data;

public class PollRepository : IPollRepository
{
    private readonly MySqlDataSource _dataSource;

    public PollRepository(MySqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<List<Poll>> GetPollsAsync(int userId)
    {
        var polls = new List<Poll>();
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT p_key, p_title, p_description, p_position, p_user_key FROM a_polls WHERE p_user_key=@userId ORDER BY p_position ASC";
        command.Parameters.AddWithValue("@userId", userId);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            polls.Add(MapPoll(reader));
        }
        return polls;
    }

    public async Task<Poll?> GetPollAsync(int userId, int pollId)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT p_key, p_title, p_description, p_position, p_user_key FROM a_polls WHERE p_user_key=@userId AND p_key=@pollId";
        command.Parameters.AddWithValue("@userId", userId);
        command.Parameters.AddWithValue("@pollId", pollId);
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapPoll(reader) : null;
    }

    public async Task<Poll?> GetPollByIdAsync(int pollId)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT p_key, p_title, p_description, p_position, p_user_key FROM a_polls WHERE p_key=@pollId";
        command.Parameters.AddWithValue("@pollId", pollId);
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? MapPoll(reader) : null;
    }

    public async Task<bool> AddPollAsync(Poll poll)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO a_polls(p_title, p_description, p_position, p_user_key) VALUES(@title, @description, @position, @userId)";
        command.Parameters.AddWithValue("@title", poll.Title);
        command.Parameters.AddWithValue("@description", poll.Description);
        command.Parameters.AddWithValue("@position", poll.Position);
        command.Parameters.AddWithValue("@userId", poll.UserId);
        return await command.ExecuteNonQueryAsync() == 1;
    }

    public async Task<bool> UpdatePollAsync(Poll poll)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE a_polls SET p_title=@title, p_description=@description, p_position=@position WHERE p_key=@pollId AND p_user_key=@userId";
        command.Parameters.AddWithValue("@title", poll.Title);
        command.Parameters.AddWithValue("@description", poll.Description);
        command.Parameters.AddWithValue("@position", poll.Position);
        command.Parameters.AddWithValue("@pollId", poll.Id);
        command.Parameters.AddWithValue("@userId", poll.UserId);
        return await command.ExecuteNonQueryAsync() == 1;
    }

    public async Task<bool> DeletePollAsync(int userId, int pollId)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM a_polls WHERE p_key=@pollId AND p_user_key=@userId";
        command.Parameters.AddWithValue("@pollId", pollId);
        command.Parameters.AddWithValue("@userId", userId);
        return await command.ExecuteNonQueryAsync() == 1;
    }

    private static Poll MapPoll(MySqlDataReader reader) => new()
    {
        Id = reader.GetInt32("p_key"),
        Title = reader.GetString("p_title"),
        Description = reader.GetString("p_description"),
        Position = reader.GetInt32("p_position"),
        UserId = reader.GetInt32("p_user_key")
    };
}
