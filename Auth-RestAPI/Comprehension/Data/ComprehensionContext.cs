using Comprehension.Models;
using Microsoft.EntityFrameworkCore;

namespace Comprehension.Data
{
    public class ComprehensionContext : DbContext
    {
        public ComprehensionContext(DbContextOptions<ComprehensionContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Note> Note { get; set; }
        public DbSet<Event> Event { get; set; }
        public DbSet<Reminder> Reminder { get; set; }
        public DbSet<SharedResource> ResourceShares { get; set; }
    }
}