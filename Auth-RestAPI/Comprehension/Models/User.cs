using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace Comprehension.Models
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public required string Username { get; set; }

        [Required]
        public required string PasswordHash { get; set; }

        [Required]
        public required string PasswordSalt { get; set; }

        public string? Token { get; set; }
        public DateTime? TokenExpiration { get; set; }

        [JsonIgnore]
        public ICollection<Note>? Notes { get; set; }
        [JsonIgnore]
        public ICollection<Event>? Events { get; set; }
        [JsonIgnore]
        public ICollection<Reminder>? Reminders { get; set; }

        public static string GenerateSalt()
        {
            var saltBytes = new byte[16];
            RandomNumberGenerator.Fill(saltBytes);
            return Convert.ToBase64String(saltBytes);
        }

        public static string HashPassword(string password, string salt)
        {
            using var sha256 = SHA256.Create();
            var combined = Encoding.UTF8.GetBytes(password + salt);
            var hash = sha256.ComputeHash(combined);
            return Convert.ToBase64String(hash);
        }

        // token aleatorio de 256 bits
        public static string GenerateToken()
        {
            var bytes = new byte[32];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToBase64String(bytes);
        }

        public bool VerifyPassword(string password)
        {
            var hashAttempt = HashPassword(password, PasswordSalt);
            return hashAttempt == PasswordHash;
        }
    }
}