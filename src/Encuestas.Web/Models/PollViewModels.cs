using System.ComponentModel.DataAnnotations;
using Encuestas.Data;
using Encuestas.Model;

namespace Encuestas.Web.Models;

public class PollFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(250, ErrorMessage = ValidationMessages.MaxLength)]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(500, ErrorMessage = ValidationMessages.MaxLength)]
    public string Description { get; set; } = string.Empty;

    [Range(1, 999999, ErrorMessage = ValidationMessages.Range)]
    public int Position { get; set; }

    public static PollFormViewModel From(Poll poll) => new()
    {
        Id = poll.Id,
        Title = poll.Title,
        Description = poll.Description,
        Position = poll.Position
    };

    /// <summary>Solo copia los campos editables; el dueño sale siempre de la sesión.</summary>
    public Poll ToPoll(int userId) => new()
    {
        Id = Id,
        Title = Title,
        Description = Description,
        Position = Position,
        UserId = userId
    };
}

public class AnswerFormViewModel
{
    [Range(1, 5, ErrorMessage = ValidationMessages.Range)]
    public int Stars { get; set; }

    [StringLength(1000, ErrorMessage = ValidationMessages.MaxLength)]
    public string? Comment { get; set; }
}

/// <summary>Perfil público: datos del usuario y una página de sus encuestas.</summary>
public record ProfileViewModel(UserProfile Profile, PagedResult<Poll> Polls);

/// <summary>Respuestas recibidas por una encuesta del usuario.</summary>
public record AnswersViewModel(Poll Poll, PagedResult<Answer> Answers);

/// <summary>Datos que necesita la vista parcial <c>_Pager</c> para enlazar las páginas.</summary>
public record PagerViewModel(int Page, int TotalPages, IDictionary<string, string> RouteValues)
{
    public bool HasPrevious => Page > 1;
    public bool HasNext => Page < TotalPages;

    public static PagerViewModel For<T>(PagedResult<T> result, IDictionary<string, string>? routeValues = null) =>
        new(result.Page, result.TotalPages, routeValues ?? new Dictionary<string, string>());

    /// <summary>Valores de ruta para enlazar a la página <paramref name="page"/>.</summary>
    public IDictionary<string, string> RouteFor(int page) =>
        new Dictionary<string, string>(RouteValues) { ["page"] = page.ToString(System.Globalization.CultureInfo.InvariantCulture) };
}
