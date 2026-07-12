using System.Security.Claims;
using Encuestas.Data;
using Encuestas.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Encuestas.Web.Controllers;

[Authorize]
public class AnswerController : Controller
{
    private readonly IPollRepository _polls;
    private readonly IAnswerRepository _answers;

    public AnswerController(IPollRepository polls, IAnswerRepository answers)
    {
        _polls = polls;
        _answers = answers;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Formulario para responder una encuesta (propia o de otro usuario).</summary>
    [HttpGet]
    public async Task<IActionResult> Add(int id)
    {
        var poll = await _polls.GetPollByIdAsync(id);
        if (poll is null)
        {
            return NotFound();
        }
        ViewData["Title"] = "Responder encuesta";
        return View("Form", poll);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int id, string? stars, string? comment)
    {
        var poll = await _polls.GetPollByIdAsync(id);
        if (poll is null)
        {
            return NotFound();
        }

        if (!int.TryParse(stars, out var parsedStars) || parsedStars < 1 || parsedStars > 5
            || (comment is not null && comment.Length > 1000))
        {
            TempData["Message"] = "Ocurrió un error en la validación de los datos";
            return RedirectToAction("Add", new { id });
        }

        var answer = new Answer
        {
            Stars = parsedStars,
            Comment = comment ?? string.Empty,
            PollId = id,
            UserId = CurrentUserId
        };
        TempData["Message"] = await _answers.AddAnswerAsync(answer)
            ? "La respuesta se guardó exitosamente"
            : "Ocurrió un error al guardar la respuesta";
        return RedirectToAction("Index", "Poll");
    }

    /// <summary>Respuestas recibidas por una encuesta; solo visibles para su propietario.</summary>
    [HttpGet]
    public async Task<IActionResult> Answers(int id)
    {
        var poll = await _polls.GetPollAsync(CurrentUserId, id);
        if (poll is null)
        {
            return NotFound();
        }
        ViewData["Title"] = $"Respuestas: {poll.Title}";
        var answers = await _answers.GetAnswersForPollAsync(id);
        return View(answers);
    }
}
