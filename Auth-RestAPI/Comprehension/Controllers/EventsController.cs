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
    public class EventsController : ControllerBase
    {
        private readonly ComprehensionContext _db;
        public EventsController(ComprehensionContext db) => _db = db;

        private User CurrentUser => (User)HttpContext.Items["User"]!;

        // GET: api/Events
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Event>>> GetEvents()
        {
            var id = CurrentUser.Id;

            var sharedIds = await _db.ResourceShares
                .Where(s => s.TargetUserId == id && s.ResourceType == "Event")
                .Select(s => s.ResourceId)
                .ToListAsync();

            return await _db.Event
                .Where(e => e.UserId == id || sharedIds.Contains(e.Id))
                .ToListAsync();
        }

        // GET: api/Events/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Event>> GetEvent(Guid id)
        {
            var ev = await _db.Event.FindAsync(id);
            if (ev == null) return NotFound();

            if (!HasAccess(ev, "read")) return StatusCode(403, new { message = "Forbidden" });

            return ev;
        }

        // POST: api/Events
        [HttpPost]
        public async Task<ActionResult<Event>> PostEvent(Event ev)
        {
            ev.Id = Guid.NewGuid();
            ev.UserId = CurrentUser.Id;

            _db.Event.Add(ev);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEvent), new { id = ev.Id }, ev);
        }

        // PUT: api/Events/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutEvent(Guid id, Event updated)
        {
            var ev = await _db.Event.FindAsync(id);
            if (ev == null) return NotFound();

            if (!HasAccess(ev, "write")) return Forbid();

            ev.Title = updated.Title;
            ev.Description = updated.Description;
            ev.StartTime = updated.StartTime;
            ev.EndTime = updated.EndTime;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/Events/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvent(Guid id)
        {
            var ev = await _db.Event.FindAsync(id);
            if (ev == null) return NotFound();

            if (!HasAccess(ev, "admin")) return Forbid();

            _db.Event.Remove(ev);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // POST: api/Events/{id}/share
        [HttpPost("{id}/share")]
        public async Task<IActionResult> ShareEvent(Guid id, [FromQuery] Guid targetUserId, [FromQuery] string role = "read")
        {
            var ev = await _db.Event.FindAsync(id);
            if (ev == null) return NotFound("Evento no encontrado");

            if (ev.UserId != CurrentUser.Id) return Forbid();

            var existing = await _db.ResourceShares.FirstOrDefaultAsync(s =>
                s.ResourceId == id && s.TargetUserId == targetUserId && s.ResourceType == "Event");

            if (existing != null)
                return Conflict("El usuario ya tiene acceso a este evento");

            var share = new SharedResource()
            {
                OwnerId = CurrentUser.Id,
                TargetUserId = targetUserId,
                ResourceId = id,
                ResourceType = "Event",
                Role = role
            };

            _db.ResourceShares.Add(share);
            await _db.SaveChangesAsync();

            return Ok($"Evento compartido con usuario {targetUserId} con rol {role}");
        }

        // GET: api/Events/{id}/share
        [HttpGet("{id}/share")]
        public async Task<ActionResult<IEnumerable<SharedResource>>> GetSharedUsers(Guid id)
        {
            var ev = await _db.Event.FindAsync(id);
            if (ev == null) return NotFound();

            if (ev.UserId != CurrentUser.Id) return Forbid();

            return await _db.ResourceShares
                .Where(s => s.ResourceId == id && s.ResourceType == "Event")
                .ToListAsync();
        }

        // DELETE: api/Events/{id}/share/{targetUserId}
        [HttpDelete("{id}/share/{targetUserId}")]
        public async Task<IActionResult> RevokeShare(Guid id, Guid targetUserId)
        {
            var ev = await _db.Event.FindAsync(id);
            if (ev == null) return NotFound("Evento no encontrado");

            if (ev.UserId != CurrentUser.Id) return Forbid();

            var share = await _db.ResourceShares.FirstOrDefaultAsync(s =>
                s.ResourceId == id && s.TargetUserId == targetUserId && s.ResourceType == "Event");

            if (share == null) return NotFound("No se encontró acceso compartido.");

            _db.ResourceShares.Remove(share);
            await _db.SaveChangesAsync();

            return Ok($"Acceso revocado para el usuario {targetUserId}");
        }

        
        //permisos
        private bool HasAccess(Event ev, string required)
        {
            if (ev.UserId == CurrentUser.Id)
                return true;

            var share = _db.ResourceShares.FirstOrDefault(s =>
                s.TargetUserId == CurrentUser.Id &&
                s.ResourceId == ev.Id &&
                s.ResourceType == "Event");

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
