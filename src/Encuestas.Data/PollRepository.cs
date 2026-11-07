using Dapper;
using Encuestas.Model;
using MySqlConnector;

namespace Encuestas.Data;

public class PollRepository(MySqlDataSource dataSource) : IPollRepository
{
    private const string Columns =
        "p_key AS Id, p_title AS Title, p_description AS Description, p_position AS Position, p_user_key AS UserId";

    public async Task<PagedResult<Poll>> GetPollsAsync(int userId, int page, int pageSize)
    {
        (page, pageSize) = Paging.Normalize(page, pageSize);
        await using var connection = await dataSource.OpenConnectionAsync();
        var total = await connection.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM a_polls WHERE p_user_key=@userId", new { userId });
        var polls = await connection.QueryAsync<Poll>(
            "SELECT " + Columns + " FROM a_polls WHERE p_user_key=@userId " +
            "ORDER BY p_position ASC, p_key ASC LIMIT @limit OFFSET @offset",
            new { userId, limit = pageSize, offset = (page - 1) * pageSize });
        return new PagedResult<Poll>(polls.AsList(), page, pageSize, total);
    }

    public async Task<Poll?> GetPollAsync(int userId, int pollId)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<Poll>(
            "SELECT " + Columns + " FROM a_polls WHERE p_user_key=@userId AND p_key=@pollId", new { userId, pollId });
    }

    public async Task<Poll?> GetPollByIdAsync(int pollId)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.QuerySingleOrDefaultAsync<Poll>(
            "SELECT " + Columns + " FROM a_polls WHERE p_key=@pollId", new { pollId });
    }

    public Task<bool> AddPollAsync(Poll poll) =>
        ExecuteAsync("INSERT INTO a_polls(p_title, p_description, p_position, p_user_key) " +
                     "VALUES(@Title, @Description, @Position, @UserId)", poll);

    public Task<bool> UpdatePollAsync(Poll poll) =>
        ExecuteAsync("UPDATE a_polls SET p_title=@Title, p_description=@Description, p_position=@Position " +
                     "WHERE p_key=@Id AND p_user_key=@UserId", poll);

    public Task<bool> DeletePollAsync(int userId, int pollId) =>
        ExecuteAsync("DELETE FROM a_polls WHERE p_key=@pollId AND p_user_key=@userId", new { userId, pollId });

    private async Task<bool> ExecuteAsync(string sql, object parameters)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        return await connection.ExecuteAsync(sql, parameters) == 1;
    }
}
