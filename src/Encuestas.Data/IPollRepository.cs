using Encuestas.Model;

namespace Encuestas.Data;

public interface IPollRepository
{
    /// <summary>Página de encuestas de un usuario, ordenadas por posición.</summary>
    Task<PagedResult<Poll>> GetPollsAsync(int userId, int page, int pageSize);

    /// <summary>Una encuesta concreta, validando que pertenezca al usuario.</summary>
    Task<Poll?> GetPollAsync(int userId, int pollId);

    /// <summary>Una encuesta por id, sin validar propietario (para responderla).</summary>
    Task<Poll?> GetPollByIdAsync(int pollId);

    Task<bool> AddPollAsync(Poll poll);
    Task<bool> UpdatePollAsync(Poll poll);
    Task<bool> DeletePollAsync(int userId, int pollId);
}
