using System.Text.Json.Serialization;

namespace Comprehension.Models
{
    public class Note
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public required string Title { get; set; }
        public required string Content { get; set; }
        [JsonIgnore] 
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        [JsonIgnore] 
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        [JsonIgnore] 
        public Guid UserId { get; set; } // dueño de la nota
        [JsonIgnore] 
        public User? User { get; set; }
    }
}