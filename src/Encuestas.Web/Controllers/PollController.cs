using Encuestas.Data;
using Encuestas.Model;
using Encuestas.Web.Models;
using Encuestas.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Encuestas.Web.Controllers;

[Authorize]
public class PollController(IPollRepository polls, ILogger<PollController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int page = 1)
    {
        ViewBag.Message = TempData["Message"];
        return View(await polls.GetPollsAsync(User.GetUserId(), page, Paging.DefaultPageSize));
    }

    [HttpGet]
    public IActionResult Add() => View("Form", new PollFormViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(PollFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Message"] = Messages.ValidationError;
            return RedirectToAction("Index");
        }

        TempData["Message"] = await polls.AddPollAsync(model.ToPoll(User.GetUserId())) ? Messages.PollSaved : Messages.PollSaveError;
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var poll = await polls.GetPollAsync(User.GetUserId(), id);
        return poll is null ? NotFound() : View("Form", PollFormViewModel.From(poll));
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

        TempData["Message"] = await polls.UpdatePollAsync(model.ToPoll(User.GetUserId())) ? Messages.PollUpdated : Messages.UpdateError;
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = User.GetUserId();
        if (id >= 1 && await polls.DeletePollAsync(userId, id))
        {
            logger.LogInformation("Encuesta {PollId} eliminada por el usuario {UserId}", id, userId);
            TempData["Message"] = Messages.PollDeleted;
        }
        else
        {
            TempData["Message"] = id < 1 ? Messages.ValidationError : Messages.PollDeleteError;
        }
        return RedirectToAction("Index");
    }
}
