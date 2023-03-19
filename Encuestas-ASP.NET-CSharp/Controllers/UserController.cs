using Data;
using Utilities;
using Model;
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

        [HttpPost]
        public ActionResult DeleteAccount(String password)
        {
            if (System.Web.HttpContext.Current.Session["id"] != null)
            {
                if (!password.Equals("") && password.Length <= 50)
                {
                    var user = userDTO.GetUserPassword((Int32)System.Web.HttpContext.Current.Session["id"]);
                    if (user.Password.Equals(Encryption.GetSHA1(password)))
                    {
                        if (userDTO.DeleteAccount((Int32)System.Web.HttpContext.Current.Session["id"]))
                        {
                            System.Web.HttpContext.Current.Session["userName"] = null;
                            System.Web.HttpContext.Current.Session["name"] = null;
                            System.Web.HttpContext.Current.Session["email"] = null;
                            System.Web.HttpContext.Current.Session["id"] = null;
                            System.Web.HttpContext.Current.Session["message"] = "La cuenta se elimino exitosamente";
                            return RedirectToAction("Index", "User");
                        }
                        else
                        {
                            ViewBag.Message = "Ocurrio un error al eliminar la cuenta";
                        }
                    }
                    else
                    {
                        ViewBag.Message = "La contraseña es incorrecta";
                    }
                }
                else
                {
                    ViewBag.Message = "Ocurrio un error en la validación de los datos";
                }
                ViewBag.ApplicationName = "Encuestas";
                ViewBag.Title = "Borrar cuenta";
                ViewBag.Path = "/User/DeleteAccount";
                return View();
            }
            else
            {
                return RedirectToAction("Index", "User");
            }
        }

        [HttpGet]
        public ActionResult DeleteAccount()
        {
            if (System.Web.HttpContext.Current.Session["id"] != null)
            {
                ViewBag.ApplicationName = "Encuestas";
                ViewBag.Title = "Borrar cuenta";
                ViewBag.Path = "/User/DeleteAccount";
                return View();
            }
            else
            {
                return RedirectToAction("Index", "User");
            }
        }

        [HttpPost]
        public ActionResult ChangePassword(String newPassword, String reNewPassword, String password)
        {
            if (System.Web.HttpContext.Current.Session["id"] != null)
            {
                if (!newPassword.Equals("") && newPassword.Length <= 50 && newPassword.Equals(reNewPassword) && !password.Equals("") && password.Length <= 50)
                {
                    var user = userDTO.GetUserPassword((Int32)System.Web.HttpContext.Current.Session["id"]);
                    if (user.Password.Equals(Encryption.GetSHA1(password)))
                    {
                        User u = new User();
                        u.Id = (Int32)System.Web.HttpContext.Current.Session["id"];
                        u.Password = Encryption.GetSHA1(newPassword);
                        if (userDTO.ChangePassword(u))
                        {
                            ViewBag.Message = "Los datos se actualizaron exitosamente";
                        }
                        else
                        {
                            ViewBag.Message = "Ocurrio un error al actualizar los datos";
                        }
                    }
                    else
                    {
                        ViewBag.Message = "La contraseña es incorrecta";
                    }
                }
                else
                {
                    ViewBag.Message = "Ocurrio un error en la validación de los datos";
                }
                ViewBag.ApplicationName = "Encuestas";
                ViewBag.Title = "Cambiar contraseña";
                ViewBag.Path = "/User/ChangePassword";
                return View();
            }
            else
            {
                return RedirectToAction("Index", "User");
            }
        }

        [HttpGet]
        public ActionResult ChangePassword()
        {
            if (System.Web.HttpContext.Current.Session["id"] != null)
            {
                ViewBag.ApplicationName = "Encuestas";
                ViewBag.Title = "Cambiar contraseña";
                ViewBag.Path = "/User/ChangePassword";
                return View();
            }
            else
            {
                return RedirectToAction("Index", "User");
            }
        }

        [HttpPost]
        public ActionResult ChangeEmail(String email, String password)
        {
            if (System.Web.HttpContext.Current.Session["id"] != null)
            {
                if (!email.Equals("") && email.Length <= 50 && !password.Equals("") && password.Length <= 50)
                {
                    var user = userDTO.GetUserPassword((Int32)System.Web.HttpContext.Current.Session["id"]);
                    if (user.Password.Equals(Encryption.GetSHA1(password)))
                    {
                        User u = new User();
                        u.Id = (Int32)System.Web.HttpContext.Current.Session["id"];
                        u.Email = email;
                        if (userDTO.ChangeEmail(u))
                        {
                            System.Web.HttpContext.Current.Session["email"] = email;
                            ViewBag.Message = "Los datos se actualizaron exitosamente";
                        }
                        else
                        {
                            ViewBag.Message = "Ocurrio un error al actualizar los datos";
                        }
                    }
                    else
                    {
                        ViewBag.Message = "La contraseña es incorrecta";
                    }
                }
                else
                {
                    ViewBag.Message = "Ocurrio un error en la validación de los datos";
                }
                ViewBag.ApplicationName = "Encuestas";
                ViewBag.Title = "Cambiar correo";
                ViewBag.Path = "/User/ChangeEmail";
                return View();
            }
            else
            {
                return RedirectToAction("Index", "User");
            }
        }

        [HttpGet]
        public ActionResult ChangeEmail()
        {
            if (System.Web.HttpContext.Current.Session["id"] != null)
            {
                ViewBag.ApplicationName = "Encuestas";
                ViewBag.Title = "Cambiar correo";
                ViewBag.Path = "/User/ChangeEmail";
                return View();
            }
            else
            {
                return RedirectToAction("Index", "User");
            }
        }

        [HttpPost]
        public ActionResult ChangeUser(String userName,String password)
        {
            if (System.Web.HttpContext.Current.Session["id"] != null)
            {
                if (!userName.Equals("") && userName.Length <= 25 && !password.Equals("") && password.Length <= 50)
                {
                    var user = userDTO.GetUserPassword((Int32)System.Web.HttpContext.Current.Session["id"]);
                    if (user.Password.Equals(Encryption.GetSHA1(password)))
                    {
                        UserProfile up = new UserProfile();
                        up.UserName = userName;
                        User u = new User();
                        u.Id = (Int32)System.Web.HttpContext.Current.Session["id"];
                        up.User = u;
                        if (userDTO.ChangeUser(up))
                        {
                            System.Web.HttpContext.Current.Session["userName"] = userName;
                            ViewBag.Message = "Los datos se actualizaron exitosamente";
                        }
                        else
                        {
                            ViewBag.Message = "Ocurrio un error al actualizar los datos";
                        }
                    }
                    else
                    {
                        ViewBag.Message = "La contraseña es incorrecta";
                    }
                }
                else
                {
                    ViewBag.Message = "Ocurrio un error en la validación de los datos";
                }
                ViewBag.ApplicationName = "Encuestas";
                ViewBag.Title = "Cambiar usuario";
                ViewBag.Path = "/User/ChangeUser";
                return View();
            }
            else
            {
                return RedirectToAction("Index", "User");
            }
        }

        [HttpGet]
        public ActionResult ChangeUser()
        {
            if (System.Web.HttpContext.Current.Session["id"] != null)
            {
                ViewBag.ApplicationName = "Encuestas";
                ViewBag.Title = "Cambiar usuario";
                ViewBag.Path = "/User/ChangeUser";
                return View();
            }
            else
            {
                return RedirectToAction("Index", "User");
            }
        }

        [HttpPost]
        public ActionResult EditProfile(String name)
        {
            if (System.Web.HttpContext.Current.Session["id"] != null)
            {
                if (!name.Equals("") && name.Length <= 50)
                {
                    UserProfile up = new UserProfile();
                    up.Name = name;
                    User u = new User();
                    u.Id = (Int32)System.Web.HttpContext.Current.Session["id"];
                    up.User = u;
                    if (userDTO.EditProfile(up))
                    {
                        System.Web.HttpContext.Current.Session["name"] = name;
                        ViewBag.Message = "Los datos se actualizaron exitosamente";
                    }
                    else
                    {
                        ViewBag.Message = "Ocurrio un error al actualizar los datos";
                    }
                }
                else
                {
                    ViewBag.Message = "Ocurrio un error en la validación de los datos";
                }
                ViewBag.ApplicationName = "Encuestas";
                ViewBag.Title = "Editar perfil";
                ViewBag.Path = "/User/EditProfile";
                return View();
            }
            else
            {
                return RedirectToAction("Index", "User");
            }
        }

        [HttpGet]
        public ActionResult EditProfile()
        {
            if (System.Web.HttpContext.Current.Session["id"] != null)
            {
                ViewBag.ApplicationName = "Encuestas";
                ViewBag.Title = "Editar perfil";
                ViewBag.Path = "/User/EditProfile";
                return View();
            }
            else
            {
                return RedirectToAction("Index", "User");
            }
        }

        [HttpGet]
        public ActionResult CloseSession()
        {
            if (System.Web.HttpContext.Current.Session["id"] != null)
            {
                System.Web.HttpContext.Current.Session["userName"] = null;
                System.Web.HttpContext.Current.Session["name"] = null;
                System.Web.HttpContext.Current.Session["email"] = null;
                System.Web.HttpContext.Current.Session["id"] = null;
                return RedirectToAction("Index", "User");
            }
            else
            {
                return RedirectToAction("Index", "User");
            }
        }

        [HttpGet]
        public ActionResult Index(String profile)
        {
            if (System.Web.HttpContext.Current.Session["id"] == null)
            {
                if (System.Web.HttpContext.Current.Session["message"] != null)
                {
                    ViewBag.Message = System.Web.HttpContext.Current.Session["message"];
                    System.Web.HttpContext.Current.Session["message"] = null;
                }
                ViewBag.ApplicationName = "Encuestas";
                ViewBag.Title = "Inicio";
                ViewBag.Path = "/";
                return View();
            }
            else
            {
                if (profile == null)
                {
                    return RedirectToAction("Index", "Poll");
                }
                else
                {
                    ViewBag.ApplicationName = "Encuestas";
                    var up = userDTO.Profile(profile);
                    if (up.User != null)
                    {
                        if (System.Web.HttpContext.Current.Session["message"] != null)
                        {
                            ViewBag.Message = System.Web.HttpContext.Current.Session["message"];
                            System.Web.HttpContext.Current.Session["message"] = null;
                        }
                        var polls = new PollDTO().GetPolls(up.User.Id);
                        ViewBag.Title = up.Name + " (" + up.UserName + ")";
                        return View("Profile", polls);
                    }
                    else
                    {
                        ViewBag.Title = "Error 404";
                        return View("../Error404");
                    }
                }
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
                            ViewBag.Message = "Correo o contraseña incorrectos";
                        }
                    }
                    else
                    {
                        ViewBag.Message = "Correo o contraseña incorrectos";
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
                            ViewBag.Message = "El registro se realizo exitosamente";
                        }
                        else
                        {
                            ViewBag.Message = "Ocurrio un error al realizar el registro";
                        }
                    }
                    else
                    {
                        ViewBag.Message = "Error en la validacíon de los datos";
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
