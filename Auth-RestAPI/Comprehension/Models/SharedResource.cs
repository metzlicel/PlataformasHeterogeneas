namespace Comprehension.Models
{
    public class SharedResource
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid OwnerId { get; set; }     // dueño del recurso
        public Guid TargetUserId { get; set; } // Usuario con quien se comparte
        public Guid ResourceId { get; set; }   // Id de la nota/evento/reminder
        public string ResourceType { get; set; } = ""; // Note, Event, Reminder
        public string Role { get; set; } = "read"; // read, write, admin
    }
}