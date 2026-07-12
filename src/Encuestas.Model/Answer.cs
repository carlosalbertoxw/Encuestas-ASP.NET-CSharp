namespace Encuestas.Model;

/// <summary>Respuesta de un usuario a una encuesta: calificación de 1 a 5 estrellas y comentario opcional.</summary>
public class Answer
{
    public int Id { get; set; }
    public int Stars { get; set; }
    public string Comment { get; set; } = string.Empty;
    public int PollId { get; set; }
    public int UserId { get; set; }

    /// <summary>Nombre de usuario de quien respondió (solo lectura, para listados).</summary>
    public string UserName { get; set; } = string.Empty;
}
