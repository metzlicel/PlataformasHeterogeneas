using Comprehension.Data;
using Comprehension.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Comprehension.Models;

namespace Comprehension.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ComprehensionContext _db;
        public AuthController(ComprehensionContext db) => _db = db;

        [HttpPost("register")]
        public async Task<IActionResult> Register(string username, string password)
        {
            if (await _db.Users.AnyAsync(u => u.Username == username))
                return BadRequest("El usuario ya existe");

            var salt = Comprehension.Models.User.GenerateSalt();
            var hash = Comprehension.Models.User.HashPassword(password, salt);

            var user = new User
            {
                Username = username,
                PasswordHash = hash,
                PasswordSalt = salt
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return Ok("Usuario registrado correctamente");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(string username, string password)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null)
                return Unauthorized("Usuario no encontrado");

            if (!user.VerifyPassword(password))
                return Unauthorized("Contraseña incorrecta");

            user.Token = Comprehension.Models.User.GenerateToken();
            user.TokenExpiration = DateTime.UtcNow.AddMinutes(60);
            await _db.SaveChangesAsync();

            return Ok(new { token = user.Token, expires = user.TokenExpiration });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromHeader(Name = "Authorization")] string? header)
        {
            var token = header?.Replace("Bearer ", "");
            if (token == null) return Unauthorized();

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Token == token);
            if (user == null) return Unauthorized();

            user.Token = null;
            user.TokenExpiration = null;
            await _db.SaveChangesAsync();

            return Ok("Sesión cerrada");
        }
        
        // solo para pruebas de compartir notas, muestra el id del usuario
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _db.Users
                .Select(u => new { u.Id, u.Username })
                .ToListAsync();

            return Ok(users);
        }

    }
}
