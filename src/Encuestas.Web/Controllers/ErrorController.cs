using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Encuestas.Web.Controllers;

[AllowAnonymous]
public class ErrorController : Controller
{
    /// <summary>Página genérica para excepciones no controladas (producción).</summary>
    [Route("/Error")]
    public IActionResult Index()
    {
        ViewData["Title"] = "Error";
        return View("Error");
    }

    /// <summary>
    /// Punto de re-ejecución de <c>UseStatusCodePagesWithReExecute</c>. El middleware conserva
    /// el código de estado original (404, 400, 500…); aquí solo se elige la vista según ese
    /// código, sin sobrescribirlo, para no enmascarar todos los errores como 404.
    /// </summary>
    [Route("/Error/NotFound")]
    public IActionResult HandleStatusCode()
    {
        if (Response.StatusCode == StatusCodes.Status404NotFound)
        {
            ViewData["Title"] = "Error 404";
            return View("Error404");
        }
        ViewData["Title"] = "Error";
        return View("Error");
    }
}
