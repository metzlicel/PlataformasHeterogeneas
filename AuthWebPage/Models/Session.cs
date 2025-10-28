using System.ComponentModel.DataAnnotations;

namespace AuthWebPage.Models;

public class Session
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(64)]
    public string SessionId { get; set; }

    [Required]
    public int UserId { get; set; }

    public DateTime LastActivity { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}