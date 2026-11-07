using Encuestas.Data;
using Encuestas.Model;
using Encuestas.Web.Models;
using Encuestas.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Encuestas.Web.Controllers;

[Authorize]
public class AnswerController(IPollRepository polls, IAnswerRepository answers) : Controller
{
    /// <summary>Formulario para responder una encuesta (propia o de otro usuario).</summary>
    [HttpGet]
    public async Task<IActionResult> Add(int id)
    {
        var poll = await polls.GetPollByIdAsync(id);
        return poll is null ? NotFound() : View("Form", poll);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int id, AnswerFormViewModel model)
    {
        if (await polls.GetPollByIdAsync(id) is null)
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
            UserId = User.GetUserId()
        };
        TempData["Message"] = await answers.AddAnswerAsync(answer) switch
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
        var poll = await polls.GetPollAsync(User.GetUserId(), id);
        if (poll is null)
        {
            return NotFound();
        }
        var result = await answers.GetAnswersForPollAsync(id, page, Paging.DefaultPageSize);
        return View(new AnswersViewModel(poll, result));
    }
}
