using System.Text.Json.Serialization;

namespace Comprehension.Models
{
    public class Reminder
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public required string Message { get; set; }
        public required DateTime ReminderTime { get; set; }
        public bool IsCompleted { get; set; } = false;
        
        [JsonIgnore] 
        public Guid UserId { get; set; } 
        [JsonIgnore] 
        public User? User { get; set; }
    }
}