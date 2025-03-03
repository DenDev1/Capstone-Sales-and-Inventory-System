using leo.Models;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace leo.Controllers
{

    public class AccountController : Controller
    {



        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        //[HttpPost]
        //public IActionResult Login(Users model)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        // Perform authentication logic here
        //        return RedirectToAction("Index", "Home");
        //    }

        //    return View(model);
        //}
        // GET: Account/AccessDenied
        public IActionResult AccessDenied()
        {
            return View();
        }

    }
}
