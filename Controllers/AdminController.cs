using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SelenicSparkApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ILogger<AdminController> _logger;

        public AdminController(ILogger<AdminController> logger)
        {
            _logger = logger;
        }

        // GET: /Admin
        public ActionResult Index()
        {
            return View();
        }

        // GET: /Users
        public ActionResult Users()
        {
            return View();
        }
    }
}
