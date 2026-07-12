using Encuestas.Model;

namespace Encuestas.Data;

public interface IAnswerRepository
{
    /// <summary>Respuestas de una encuesta, con el nombre de usuario de cada autor.</summary>
    Task<List<Answer>> GetAnswersForPollAsync(int pollId);

    Task<bool> AddAnswerAsync(Answer answer);
}
