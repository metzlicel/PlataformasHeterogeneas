using Comprehension.Attributes;
using Comprehension.Data;
using Comprehension.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Comprehension.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [AuthorizeCustom]
    public class RemindersController : ControllerBase
    {
        private readonly ComprehensionContext _db;
        public RemindersController(ComprehensionContext db) => _db = db;

        private User CurrentUser => (User)HttpContext.Items["User"]!;

        // GET: api/Reminders
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Reminder>>> GetReminders()
        {
            var id = CurrentUser.Id;
            var sharedIds = await _db.ResourceShares
                .Where(s => s.TargetUserId == id && s.ResourceType == "Reminder")
                .Select(s => s.ResourceId)
                .ToListAsync();

            return await _db.Reminder
                .Where(r => r.UserId == id || sharedIds.Contains(r.Id))
                .ToListAsync();
        }

        // GET: api/Reminders/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Reminder>> GetReminder(Guid id)
        {
            var reminder = await _db.Reminder.FindAsync(id);
            if (reminder == null) return NotFound();

            if (!HasAccess(reminder, "read")) return StatusCode(403, new { message = "Forbidden" });

            return reminder;
        }

        // POST: api/Reminders
        [HttpPost]
        public async Task<ActionResult<Reminder>> PostReminder(Reminder reminder)
        {
            reminder.Id = Guid.NewGuid();
            reminder.UserId = CurrentUser.Id;

            _db.Reminder.Add(reminder);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetReminder), new { id = reminder.Id }, reminder);
        }

        // PUT: api/Reminders/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutReminder(Guid id, Reminder updated)
        {
            var reminder = await _db.Reminder.FindAsync(id);
            if (reminder == null) return NotFound();

            if (!HasAccess(reminder, "write")) return Forbid();

            reminder.Message = updated.Message;
            reminder.ReminderTime = updated.ReminderTime;
            reminder.IsCompleted = updated.IsCompleted;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/Reminders/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReminder(Guid id)
        {
            var reminder = await _db.Reminder.FindAsync(id);
            if (reminder == null) return NotFound();

            if (!HasAccess(reminder, "admin")) return Forbid();

            _db.Reminder.Remove(reminder);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // POST: api/Reminders/{id}/share
        [HttpPost("{id}/share")]
        public async Task<IActionResult> ShareReminder(Guid id, [FromQuery] Guid targetUserId, [FromQuery] string role = "read")
        {
            var reminder = await _db.Reminder.FindAsync(id);
            if (reminder == null) return NotFound("Recordatorio no encontrado");

            if (reminder.UserId != CurrentUser.Id) return Forbid();

            var existing = await _db.ResourceShares.FirstOrDefaultAsync(s =>
                s.ResourceId == id && s.TargetUserId == targetUserId && s.ResourceType == "Reminder");

            if (existing != null)
                return Conflict("El usuario ya tiene acceso a este recordatorio");

            var share = new SharedResource()
            {
                OwnerId = CurrentUser.Id,
                TargetUserId = targetUserId,
                ResourceId = id,
                ResourceType = "Reminder",
                Role = role
            };

            _db.ResourceShares.Add(share);
            await _db.SaveChangesAsync();

            return Ok($"Recordatorio compartido con usuario {targetUserId} con rol {role}");
        }

        // GET: api/Reminders/{id}/share
        [HttpGet("{id}/share")]
        public async Task<ActionResult<IEnumerable<SharedResource>>> GetSharedUsers(Guid id)
        {
            var reminder = await _db.Reminder.FindAsync(id);
            if (reminder == null) return NotFound();

            if (reminder.UserId != CurrentUser.Id) return Forbid();

            return await _db.ResourceShares
                .Where(s => s.ResourceId == id && s.ResourceType == "Reminder")
                .ToListAsync();
        }

        // DELETE: api/Reminders/{id}/share/{targetUserId}
        [HttpDelete("{id}/share/{targetUserId}")]
        public async Task<IActionResult> RevokeShare(Guid id, Guid targetUserId)
        {
            var reminder = await _db.Reminder.FindAsync(id);
            if (reminder == null) return NotFound("Recordatorio no encontrado");

            if (reminder.UserId != CurrentUser.Id) return Forbid();

            var share = await _db.ResourceShares.FirstOrDefaultAsync(s =>
                s.ResourceId == id && s.TargetUserId == targetUserId && s.ResourceType == "Reminder");

            if (share == null) return NotFound("No se encontró acceso compartido.");

            _db.ResourceShares.Remove(share);
            await _db.SaveChangesAsync();

            return Ok($"Acceso revocado para el usuario {targetUserId}");
        }

        // Permisos
        private bool HasAccess(Reminder reminder, string required)
        {
            if (reminder.UserId == CurrentUser.Id)
                return true;

            var share = _db.ResourceShares.FirstOrDefault(s =>
                s.TargetUserId == CurrentUser.Id &&
                s.ResourceId == reminder.Id &&
                s.ResourceType == "Reminder");

            if (share == null)
                return false;

            return required switch
            {
                "read" => true,
                "write" => share.Role is "write" or "admin",
                "admin" => share.Role == "admin",
                _ => false
            };
        }
    }
}
