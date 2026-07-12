using Encuestas.Model;

namespace Encuestas.Data;

public interface IPollRepository
{
    /// <summary>Encuestas de un usuario, ordenadas por posición.</summary>
    Task<List<Poll>> GetPollsAsync(int userId);

    /// <summary>Una encuesta concreta, validando que pertenezca al usuario.</summary>
    Task<Poll?> GetPollAsync(int userId, int pollId);

    /// <summary>Una encuesta por id, sin validar propietario (para responderla).</summary>
    Task<Poll?> GetPollByIdAsync(int pollId);

    Task<bool> AddPollAsync(Poll poll);
    Task<bool> UpdatePollAsync(Poll poll);
    Task<bool> DeletePollAsync(int userId, int pollId);
}
