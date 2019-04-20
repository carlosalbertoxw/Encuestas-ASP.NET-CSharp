using Data;
using Utilities;
using System;
using System.Web.Mvc;
using System.Diagnostics;

namespace Encuestas_ASP.NET_CSharp.Controllers
{
    public class UserController : Controller
    {
        private UserDTO userDTO;

        public UserController()
        {
            userDTO = new UserDTO();
        }

        [HttpGet]
        public ActionResult Index()
        {
            if (System.Web.HttpContext.Current.Session["id"] == null)
            {
                ViewBag.ApplicationName = "Encuestas";
                ViewBag.Title = "Inicio";
                ViewBag.Path = "/";
                return View();
            }
            else
            {
                return RedirectToAction("Index", "Poll");
            }
        }

        [HttpPost]
        public ActionResult Index(String form,String email, String password,String rePassword)
        {
            if (System.Web.HttpContext.Current.Session["id"] == null)
            {
                if (form.Equals("sign-in"))
                {
                    if (!email.Equals("") && email.Length <= 50 && !password.Equals("") && password.Length <= 50)
                    {
                        var up = userDTO.GetUserProfileByEmail(email);
                        Debug.WriteLine("user name: "+up.UserName);
                        if (up.User!=null && Encryption.GetSHA1(password).Equals(up.User.Password))
                        {
                            System.Web.HttpContext.Current.Session["userName"] = up.UserName;
                            System.Web.HttpContext.Current.Session["name"] = up.Name;
                            System.Web.HttpContext.Current.Session["email"] = up.User.Email;
                            System.Web.HttpContext.Current.Session["id"] = up.User.Id;
                            return RedirectToAction("Index", "Poll");
                        }
                        else
                        {
                            ViewBag.message = "Correo o contraseña incorrectos";
                        }
                    }
                    else
                    {
                        ViewBag.message = "Correo o contraseña incorrectos";
                    }
                }
                else
                if (form.Equals("sign-up"))
                {
                    if (!password.Equals("") && !rePassword.Equals("") && !email.Equals("") && password.Equals(rePassword) && email.Length <= 50 && password.Length <= 50)
                    {
                        password = Encryption.GetSHA1(password);
                        if (userDTO.addUser(email, password))
                        {
                            ViewBag.message = "El registro se realizo exitosamente";
                        }
                        else
                        {
                            ViewBag.message = "Ocurrio un error al realizar el registro";
                        }
                    }
                    else
                    {
                        ViewBag.message = "Error en la validacíon de los datos";
                    }
                }
                ViewBag.ApplicationName = "Encuestas";
                ViewBag.Title = "Inicio";
                ViewBag.Path = "/";
                return View();
            }
            else
            {
                return RedirectToAction("Index", "Poll");
            }
        }
    }
}
