using System.Globalization;
using System.Security.Claims;
using Encuestas.Data;
using Encuestas.Model;
using Encuestas.Web.Models;
using Encuestas.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Encuestas.Web.Controllers;

[Authorize]
public class AnswerController : Controller
{
    private const int PageSize = 10;

    private readonly IPollRepository _polls;
    private readonly IAnswerRepository _answers;

    public AnswerController(IPollRepository polls, IAnswerRepository answers)
    {
        _polls = polls;
        _answers = answers;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, CultureInfo.InvariantCulture);

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
    public async Task<IActionResult> Add(int id, AnswerFormViewModel model)
    {
        var poll = await _polls.GetPollByIdAsync(id);
        if (poll is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            TempData["Message"] = Messages.ValidationError;
            return RedirectToAction("Add", new { id });
        }

        var answer = new Answer
        {
            Stars = model.Stars,
            Comment = model.Comment ?? string.Empty,
            PollId = id,
            UserId = CurrentUserId
        };
        TempData["Message"] = await _answers.AddAnswerAsync(answer) switch
        {
            RepositoryResult.Success => Messages.AnswerSaved,
            RepositoryResult.Duplicate => Messages.AnswerDuplicate,
            _ => Messages.AnswerError
        };
        return RedirectToAction("Index", "Poll");
    }

    /// <summary>Respuestas recibidas por una encuesta; solo visibles para su propietario.</summary>
    [HttpGet]
    public async Task<IActionResult> Answers(int id, int page = 1)
    {
        var poll = await _polls.GetPollAsync(CurrentUserId, id);
        if (poll is null)
        {
            return NotFound();
        }
        ViewData["Title"] = $"Respuestas: {poll.Title}";
        ViewBag.PollId = id;
        var answers = await _answers.GetAnswersForPollAsync(id, page, PageSize);
        return View(answers);
    }
}
