using Encuestas.Model;
using MySqlConnector;

namespace Encuestas.Data;

public class AnswerRepository : IAnswerRepository
{
    private readonly MySqlDataSource _dataSource;

    public AnswerRepository(MySqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<List<Answer>> GetAnswersForPollAsync(int pollId)
    {
        var answers = new List<Answer>();
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT a.a_key, a.a_stars, a.a_comment, a.a_poll_key, a.a_user_key, up.u_p_user_name " +
                              "FROM a_answers AS a JOIN a_users_profiles AS up ON a.a_user_key = up.u_p_key " +
                              "WHERE a.a_poll_key=@pollId ORDER BY a.a_key DESC";
        command.Parameters.AddWithValue("@pollId", pollId);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            answers.Add(new Answer
            {
                Id = reader.GetInt32("a_key"),
                Stars = reader.GetInt32("a_stars"),
                Comment = reader.GetString("a_comment"),
                PollId = reader.GetInt32("a_poll_key"),
                UserId = reader.GetInt32("a_user_key"),
                UserName = reader.GetString("u_p_user_name")
            });
        }
        return answers;
    }

    public async Task<bool> AddAnswerAsync(Answer answer)
    {
        await using var connection = await _dataSource.OpenConnectionAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO a_answers(a_stars, a_comment, a_poll_key, a_user_key) VALUES(@stars, @comment, @pollId, @userId)";
        command.Parameters.AddWithValue("@stars", answer.Stars);
        command.Parameters.AddWithValue("@comment", answer.Comment);
        command.Parameters.AddWithValue("@pollId", answer.PollId);
        command.Parameters.AddWithValue("@userId", answer.UserId);
        return await command.ExecuteNonQueryAsync() == 1;
    }
}
