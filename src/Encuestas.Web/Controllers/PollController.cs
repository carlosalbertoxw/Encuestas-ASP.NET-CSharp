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
public class PollController : Controller
{
    private readonly IPollRepository _polls;
    private readonly ILogger<PollController> _logger;

    public PollController(IPollRepository polls, ILogger<PollController> logger)
    {
        _polls = polls;
        _logger = logger;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!, CultureInfo.InvariantCulture);

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        ViewData["Title"] = "Tablero";
        ViewBag.Message = TempData["Message"];
        var polls = await _polls.GetPollsAsync(CurrentUserId);
        return View(polls);
    }

    [HttpGet]
    public IActionResult Add()
    {
        ViewData["Title"] = "Agregar encuesta";
        return View("Form", new PollFormViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(PollFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Message"] = Messages.ValidationError;
            return RedirectToAction("Index");
        }

        var poll = new Poll
        {
            Title = model.Title,
            Description = model.Description,
            Position = model.Position,
            UserId = CurrentUserId
        };
        TempData["Message"] = await _polls.AddPollAsync(poll) ? Messages.PollSaved : Messages.PollSaveError;
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var poll = await _polls.GetPollAsync(CurrentUserId, id);
        if (poll is null)
        {
            return NotFound();
        }
        ViewData["Title"] = "Editar encuesta";
        return View("Form", new PollFormViewModel
        {
            Id = poll.Id,
            Title = poll.Title,
            Description = poll.Description,
            Position = poll.Position
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(PollFormViewModel model)
    {
        if (!ModelState.IsValid || model.Id < 1)
        {
            TempData["Message"] = Messages.ValidationError;
            return RedirectToAction("Index");
        }

        var poll = new Poll
        {
            Id = model.Id,
            Title = model.Title,
            Description = model.Description,
            Position = model.Position,
            UserId = CurrentUserId
        };
        TempData["Message"] = await _polls.UpdatePollAsync(poll) ? Messages.PollUpdated : Messages.UpdateError;
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        if (id < 1)
        {
            TempData["Message"] = Messages.ValidationError;
            return RedirectToAction("Index");
        }

        if (await _polls.DeletePollAsync(CurrentUserId, id))
        {
            _logger.LogInformation("Encuesta {PollId} eliminada por el usuario {UserId}", id, CurrentUserId);
            TempData["Message"] = Messages.PollDeleted;
        }
        else
        {
            TempData["Message"] = Messages.PollDeleteError;
        }
        return RedirectToAction("Index");
    }
}
