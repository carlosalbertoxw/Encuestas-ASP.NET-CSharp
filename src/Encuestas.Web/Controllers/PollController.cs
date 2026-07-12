using System.Security.Claims;
using Encuestas.Data;
using Encuestas.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Encuestas.Web.Controllers;

[Authorize]
public class PollController : Controller
{
    private readonly IPollRepository _polls;

    public PollController(IPollRepository polls)
    {
        _polls = polls;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

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
        return View("Form", new Poll());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(string? title, string? description, string? position)
    {
        if (!TryValidatePoll(title, description, position, out var parsedPosition))
        {
            TempData["Message"] = "Ocurrió un error en la validación de los datos";
            return RedirectToAction("Index");
        }

        var poll = new Poll
        {
            Title = title!,
            Description = description!,
            Position = parsedPosition,
            UserId = CurrentUserId
        };
        TempData["Message"] = await _polls.AddPollAsync(poll)
            ? "Los datos se guardaron exitosamente"
            : "Ocurrió un error al guardar los datos";
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
        return View("Form", poll);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(string? title, string? description, string? position, int id)
    {
        if (!TryValidatePoll(title, description, position, out var parsedPosition) || id < 1)
        {
            TempData["Message"] = "Ocurrió un error en la validación de los datos";
            return RedirectToAction("Index");
        }

        var poll = new Poll
        {
            Id = id,
            Title = title!,
            Description = description!,
            Position = parsedPosition,
            UserId = CurrentUserId
        };
        TempData["Message"] = await _polls.UpdatePollAsync(poll)
            ? "Los datos se actualizaron exitosamente"
            : "Ocurrió un error al actualizar los datos";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        if (id < 1)
        {
            TempData["Message"] = "Ocurrió un error en la validación de los datos";
            return RedirectToAction("Index");
        }

        TempData["Message"] = await _polls.DeletePollAsync(CurrentUserId, id)
            ? "Los datos se borraron exitosamente"
            : "Ocurrió un error al borrar los datos";
        return RedirectToAction("Index");
    }

    private static bool TryValidatePoll(string? title, string? description, string? position, out int parsedPosition)
    {
        parsedPosition = 0;
        return !string.IsNullOrWhiteSpace(title) && title.Length <= 250
            && !string.IsNullOrWhiteSpace(description) && description.Length <= 500
            && int.TryParse(position, out parsedPosition) && parsedPosition >= 1;
    }
}
