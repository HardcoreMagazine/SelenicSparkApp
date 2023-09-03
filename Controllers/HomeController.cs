using Microsoft.AspNetCore.Mvc;
using SelenicSparkApp.Models;
using System.Diagnostics;

namespace SelenicSparkApp.Controllers
{
    public class HomeController : Controller
    {
        public HomeController() { }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Contacts()
        {
            return View();
        }

        public IActionResult Rules()
        {
            return View();
        }

        /*
        public IActionResult FAQ()
        {
            return View();
        }
        */

        public IActionResult Goodbye()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int code)
        {
            if (code == 404)
            {
                return View("404");
            }
            else
            {
                return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier, Code = code });
            }
        }
    }
}