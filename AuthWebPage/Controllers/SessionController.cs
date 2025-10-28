using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;
using System.Text;
using AuthWebPage.Models;

namespace AuthWebPage.Controllers
{
    public class SessionController : Controller
    {
        private readonly AppDbContext _context;
        public SessionController(AppDbContext context)
        {
            _context = context;
        }

        private static string GenerateSalt()
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16); // 128 bits
            return Convert.ToBase64String(salt);
        }

        private static string HashPassword(string password, string salt)
        {
            // Usa PBKDF2 
            byte[] saltBytes = Convert.FromBase64String(salt);
            byte[] hash = KeyDerivation.Pbkdf2(
                password: password,
                salt: saltBytes,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000, 
                numBytesRequested: 32);  // 256 / 8 bits

            return Convert.ToBase64String(hash);
        }

        private static string GenerateSessionId()
        {
            // Generar un id aleatorio de 128 bits
            byte[] bytes = RandomNumberGenerator.GetBytes(16);
            return Convert.ToBase64String(bytes); 
        }

        private void CreateSession(User user)
        {
            var session = new Session
            {
                SessionId = GenerateSessionId(),
                UserId = user.Id,
                LastActivity = DateTime.UtcNow
            };

            _context.Sessions.Add(session);
            _context.SaveChanges();

            // Cookie segura
            Response.Cookies.Append("SessionId", session.SessionId, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });
        }

        private void InvalidateSession()
        {
            if (Request.Cookies.TryGetValue("SessionId", out var cookie))
            {
                var session = _context.Sessions.FirstOrDefault(s => s.SessionId == cookie);
                if (session != null)
                {
                    _context.Sessions.Remove(session);
                    _context.SaveChanges();
                }
                Response.Cookies.Delete("SessionId");
            }
        }

        private User? GetUserFromSession()
        {
            if (!Request.Cookies.TryGetValue("SessionId", out var cookie))
                return null;

            var session = _context.Sessions
                .Include(s => s.User)
                .FirstOrDefault(s => s.SessionId == cookie);

            if (session == null)
                return null;

            // Expiracion por inactividad
            if (DateTime.UtcNow - session.LastActivity > TimeSpan.FromMinutes(5))
            {
                _context.Sessions.Remove(session);
                _context.SaveChanges();
                Response.Cookies.Delete("SessionId");
                return null;
            }

            session.LastActivity = DateTime.UtcNow;
            _context.SaveChanges();
            return session.User;
        }

        // *** Views ******
        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public IActionResult Register(string username, string password, string name, string email, string animal)
        {
            if (_context.Users.Any(u => u.Username == username))
            {
                ViewBag.Error = "El usuario ya existe";
                return View();
            }

            var salt = GenerateSalt();
            var hash = HashPassword(password, salt);

            var user = new User
            {
                Username = username,
                PasswordHash = hash,
                Salt = salt,
                Name = name,
                Email = email,
                FavAnimal = animal
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult Login() => View();

        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == username);
            if (user == null)
            {
                ViewBag.Error = "Usuario no encontrado";
                return View();
            }

            var hash = HashPassword(password, user.Salt);
            if (hash != user.PasswordHash)
            {
                ViewBag.Error = "Contraseña incorrecta";
                return View();
            }

            CreateSession(user);
            return RedirectToAction("Privacy", "Home");
        }

        [HttpPost]
        public IActionResult Logout()
        {
            InvalidateSession();
            return RedirectToAction("Index", "Home");
        }

        // metodo para validacion desde HomeController
        public User? ValidateCurrentUser() => GetUserFromSession();
    }
}
