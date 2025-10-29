using AuthWebPage.Controllers;
using Microsoft.AspNetCore.Mvc;
using AuthWebPage.Models;
using Microsoft.EntityFrameworkCore;

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
            // Leer cookie directamente desde el contexto actual
            if (!Request.Cookies.TryGetValue("SessionId", out var sessionId))
                return View("AccessDenied");

            var session = _context.Sessions
                .Include(s => s.User)
                .FirstOrDefault(s => s.SessionId == sessionId);

            if (session == null)
                return View("AccessDenied");

            if (DateTime.UtcNow - session.LastActivity > TimeSpan.FromSeconds(5)) 
            {
                _context.Sessions.Remove(session);
                _context.SaveChanges();
                Response.Cookies.Delete("SessionId");
                return View("AccessDenied");
            }

            session.LastActivity = DateTime.UtcNow;
            _context.SaveChanges();

            return View("Privacy", session.User);
        }


    }
}