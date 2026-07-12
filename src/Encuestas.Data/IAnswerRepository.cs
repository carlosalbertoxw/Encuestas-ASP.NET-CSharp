using Encuestas.Model;

namespace Encuestas.Data;

public interface IAnswerRepository
{
    /// <summary>Página de respuestas de una encuesta (más recientes primero), con el nombre de usuario de cada autor.</summary>
    Task<PagedResult<Answer>> GetAnswersForPollAsync(int pollId, int page, int pageSize);

    /// <summary>Registra una respuesta; <see cref="RepositoryResult.Duplicate"/> si el usuario ya respondió esa encuesta.</summary>
    Task<RepositoryResult> AddAnswerAsync(Answer answer);
}
