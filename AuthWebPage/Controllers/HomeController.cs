using AuthWebPage.Controllers;
using Microsoft.AspNetCore.Mvc;
using AuthWebPage.Models;

namespace YourApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _context;

        public HomeController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index() => View();

        public IActionResult Privacy()
        {
            var auth = new SessionController(_context);
            var user = auth.ValidateCurrentUser();

            if (user == null)
                return RedirectToAction("Login", "Auth");

            return View(user);
        }
    }
}