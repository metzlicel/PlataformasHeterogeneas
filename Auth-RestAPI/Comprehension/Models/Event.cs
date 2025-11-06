using System.Text.Json.Serialization;

namespace Comprehension.Models
{
    public class Event
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public required string Title { get; set; }
        public required string Description { get; set; }
        public required DateTime StartTime { get; set; }
        public required DateTime EndTime { get; set; }

        [JsonIgnore]
        public Guid UserId { get; set; } 

        [JsonIgnore]
        public User? User { get; set; }
    }
}