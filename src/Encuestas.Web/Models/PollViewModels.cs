using System.ComponentModel.DataAnnotations;

namespace Encuestas.Web.Models;

public class PollFormViewModel
{
    public int Id { get; set; }

    [Required, StringLength(250)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(500)]
    public string Description { get; set; } = string.Empty;

    [Range(1, 999999)]
    public int Position { get; set; }
}

public class AnswerFormViewModel
{
    [Range(1, 5)]
    public int Stars { get; set; }

    [StringLength(1000)]
    public string? Comment { get; set; }
}
