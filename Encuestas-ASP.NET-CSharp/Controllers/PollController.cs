﻿using Data;
using Utilities;
using Model;
using System;
using System.Web.Mvc;
using System.Diagnostics;

namespace Encuestas_ASP.NET_CSharp.Controllers
{
    public class PollController : Controller
    {

        [HttpGet]
        public ActionResult Add()
        {
            if (System.Web.HttpContext.Current.Session["id"] != null)
            {
                ViewBag.ApplicationName = "Encuestas";
                ViewBag.Title = "Agregar encuesta";
                ViewBag.Path = "/Poll/Add";
                return View();
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
                return View();
            }
            else
            {
                return RedirectToAction("Index", "User");
            }
        }
    }
}
