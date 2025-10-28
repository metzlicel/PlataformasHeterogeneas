using System.ComponentModel.DataAnnotations;

namespace AuthWebPage.Models;

public class User
{
    [Key]
        public int Id { get; set; }

        [Required]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public string Salt { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string FavAnimal { get; set; } = string.Empty;
}