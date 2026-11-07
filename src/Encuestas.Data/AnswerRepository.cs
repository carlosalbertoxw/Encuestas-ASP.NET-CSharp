using Dapper;
using Encuestas.Model;
using MySqlConnector;

namespace Encuestas.Data;

public class AnswerRepository(MySqlDataSource dataSource) : IAnswerRepository
{
    public async Task<PagedResult<Answer>> GetAnswersForPollAsync(int pollId, int page, int pageSize)
    {
        (page, pageSize) = Paging.Normalize(page, pageSize);
        await using var connection = await dataSource.OpenConnectionAsync();
        var total = await connection.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM a_answers WHERE a_poll_key=@pollId", new { pollId });
        var answers = await connection.QueryAsync<Answer>(
            "SELECT a.a_key AS Id, a.a_stars AS Stars, a.a_comment AS Comment, a.a_poll_key AS PollId, " +
            "a.a_user_key AS UserId, up.u_p_user_name AS UserName " +
            "FROM a_answers AS a JOIN a_users_profiles AS up ON a.a_user_key = up.u_p_key " +
            "WHERE a.a_poll_key=@pollId ORDER BY a.a_key DESC LIMIT @limit OFFSET @offset",
            new { pollId, limit = pageSize, offset = (page - 1) * pageSize });
        return new PagedResult<Answer>(answers.AsList(), page, pageSize, total);
    }

    public async Task<RepositoryResult> AddAnswerAsync(Answer answer)
    {
        await using var connection = await dataSource.OpenConnectionAsync();
        try
        {
            var rows = await connection.ExecuteAsync(
                "INSERT INTO a_answers(a_stars, a_comment, a_poll_key, a_user_key) VALUES(@Stars, @Comment, @PollId, @UserId)",
                answer);
            return rows == 1 ? RepositoryResult.Success : RepositoryResult.NotFound;
        }
        catch (MySqlException ex) when (ex.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
        {
            return RepositoryResult.Duplicate;
        }
    }
}
