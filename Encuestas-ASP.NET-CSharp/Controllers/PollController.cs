using Data;
using Model;
using System;
using System.Web.Mvc;

namespace Encuestas_ASP.NET_CSharp.Controllers
{
    public class PollController : Controller
    {

        private PollDTO pollDTO;

        public PollController()
        {
            pollDTO = new PollDTO();
        }

        [HttpGet]
        public ActionResult Delete(string id)
        {
            if (System.Web.HttpContext.Current.Session["id"] != null)
            {
                if (id.Length <= 6 && Int32.Parse(id) >= 1)
                {
                    Poll poll = new Poll();
                    poll.Id = Int32.Parse(id);
                    User u = new User();
                    u.Id = (Int32)System.Web.HttpContext.Current.Session["id"];
                    poll.User = u;
                    if (pollDTO.DeletePoll(poll) == 1)
                    {
                        System.Web.HttpContext.Current.Session["message"] = "Los datos se borraron exitosamente";
                    }
                    else
                    {
                        System.Web.HttpContext.Current.Session["message"] = "Ocurrio un error al borraron los datos";
                    }
                }
                else
                {
                    System.Web.HttpContext.Current.Session["message"] = "Ocurrio un error en la validación de los datos";
                }
                return RedirectToAction("Index", "User");
            }
            else
            {
                return RedirectToAction("Index", "User");
            }
        }

        [HttpPost]
        public ActionResult Update(String title, String description, string position, string id)
        {
            if (System.Web.HttpContext.Current.Session["id"] != null)
            {
                if (!title.Equals("") && title.Length <= 250
                    && !description.Equals("") && description.Length <= 500
                    && !position.Equals("") && position.Length <= 6 && Int32.Parse(position) >= 1
                    && id.Length <= 6 && Int32.Parse(id) >= 1)
                {
                    Poll poll = new Poll();
                    poll.Id = Int32.Parse(id);
                    poll.Title = title;
                    poll.Description = description;
                    poll.Position = Int32.Parse(position);
                    User u = new User();
                    u.Id = (Int32)System.Web.HttpContext.Current.Session["id"];
                    poll.User = u;
                    if (pollDTO.UpdatePoll(poll) == 1)
                    {
                        System.Web.HttpContext.Current.Session["message"] = "Los datos se actualizaron exitosamente";
                    }
                    else
                    {
                        System.Web.HttpContext.Current.Session["message"] = "Ocurrio un error al actualizaron los datos";
                    }
                }
                else
                {
                    System.Web.HttpContext.Current.Session["message"] = "Ocurrio un error en la validación de los datos";
                }
                return RedirectToAction("Index", "User");
            }
            else
            {
                return RedirectToAction("Index", "User");
            }
        }

        [HttpGet]
        public ActionResult Edit(String id)
        {
            if (System.Web.HttpContext.Current.Session["id"] != null)
            {
                if (Int32.Parse(id)>0)
                {
                    ViewBag.ApplicationName = "Encuestas";
                    ViewBag.Title = "Editar encuesta";
                    ViewBag.Path = "/Poll/Update";
                    var poll = new PollDTO().GetPoll((Int32)System.Web.HttpContext.Current.Session["id"], Int32.Parse(id));
                    return View("Form",poll);
                }
                else
                {
                    return RedirectToAction("Index", "Poll");
                }
            }
            else
            {
                return RedirectToAction("Index", "User");
            }
        }

        [HttpPost]
        public ActionResult Add(String title, String description, string position)
        {
            if (System.Web.HttpContext.Current.Session["id"] != null)
            {
                if (!title.Equals("") && title.Length <= 250
                    && !description.Equals("") && description.Length <= 500
                    && !position.Equals("") && position.Length <= 6 && Int32.Parse(position)>=1)
                {
                    Poll poll = new Poll();
                    poll.Title = title;
                    poll.Description = description;
                    poll.Position = Int32.Parse(position);
                    User u = new User();
                    u.Id = (Int32)System.Web.HttpContext.Current.Session["id"];
                    poll.User = u;
                    if (pollDTO.AddPoll(poll)==1)
                    {
                        System.Web.HttpContext.Current.Session["message"] = "Los datos se guardaron exitosamente";
                    }
                    else
                    {
                        System.Web.HttpContext.Current.Session["message"] = "Ocurrio un error al guardar los datos";
                    }
                }
                else
                {
                    System.Web.HttpContext.Current.Session["message"] = "Ocurrio un error en la validación de los datos";
                }
                return RedirectToAction("Index", "Poll");
            }
            else
            {
                return RedirectToAction("Index", "User");
            }
        }

        [HttpGet]
        public ActionResult Add()
        {
            if (System.Web.HttpContext.Current.Session["id"] != null)
            {
                ViewBag.ApplicationName = "Encuestas";
                ViewBag.Title = "Agregar encuesta";
                ViewBag.Path = "/Poll/Form";
                return View("Form", new Poll());
            }
            else
            {
                return RedirectToAction("Index", "User");
            }
        }

        [HttpGet]
        public ActionResult Index()
        {
            if (System.Web.HttpContext.Current.Session["id"] != null)
            {
                ViewBag.ApplicationName = "Encuestas";
                ViewBag.Title = "Tablero";
                ViewBag.Path = "/Poll";
                ViewBag.Message = System.Web.HttpContext.Current.Session["message"];
                System.Web.HttpContext.Current.Session["message"] = null;
                var polls = new PollDTO().GetPolls((Int32)System.Web.HttpContext.Current.Session["id"]);
                return View(polls);
            }
            else
            {
                return RedirectToAction("Index", "User");
            }
        }
    }
}
