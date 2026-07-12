using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Encuestas.Web.Controllers;

[AllowAnonymous]
public class ErrorController : Controller
{
    /// <summary>Página genérica para errores no controlados (producción).</summary>
    [Route("/Error")]
    public IActionResult Index()
    {
        ViewData["Title"] = "Error";
        return View("Error");
    }

    /// <summary>Página para respuestas 404, re-ejecutada por UseStatusCodePagesWithReExecute.</summary>
    [Route("/Error/NotFound")]
    public new IActionResult NotFound()
    {
        ViewData["Title"] = "Error 404";
        Response.StatusCode = StatusCodes.Status404NotFound;
        return View("Error404");
    }
}
