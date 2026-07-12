namespace Encuestas.Model;

/// <summary>Encuesta creada por un usuario.</summary>
public class Poll
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>Orden de aparición en el tablero y en el perfil público.</summary>
    public int Position { get; set; }

    public int UserId { get; set; }
}
