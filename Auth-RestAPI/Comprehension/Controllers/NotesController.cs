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
    public class NotesController : ControllerBase
    {
        private readonly ComprehensionContext _db;
        public NotesController(ComprehensionContext db) => _db = db;

        private User CurrentUser => (User)HttpContext.Items["User"]!;

        // GET: api/Notes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Note>>> GetNotes()
        {
            var id = CurrentUser.Id;

            // Notas propias y compartidas
            var sharedIds = await _db.ResourceShares
                .Where(s => s.TargetUserId == id && s.ResourceType == "Note")
                .Select(s => s.ResourceId)
                .ToListAsync();

            return await _db.Note
                .Where(n => n.UserId == id || sharedIds.Contains(n.Id))
                .ToListAsync();
        }

        // GET: api/Notes/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<Note>> GetNote(Guid id)
        {
            var note = await _db.Note.FindAsync(id);
            if (note == null) return NotFound();

            if (!HasAccess(note, "read")) return StatusCode(403, new { message = "Forbidden" });

            return note;
        }

        // POST: api/Notes
        [HttpPost]
        public async Task<ActionResult<Note>> PostNote(Note note)
        {
            note.Id = Guid.NewGuid();
            note.UserId = CurrentUser.Id;
            note.CreatedAt = DateTime.UtcNow;
            note.UpdatedAt = DateTime.UtcNow;

            _db.Note.Add(note);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetNote), new { id = note.Id }, note);
        }

        // PUT: api/Notes/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> PutNote(Guid id, Note updated)
        {
            var note = await _db.Note.FindAsync(id);
            if (note == null) return NotFound();

            if (!HasAccess(note, "write")) return Forbid();

            note.Title = updated.Title;
            note.Content = updated.Content;
            note.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/Notes/{id}
        
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNote(Guid id)
        {
            var note = await _db.Note.FindAsync(id);
            if (note == null) return NotFound();

            if (!HasAccess(note, "admin")) return Forbid();

            _db.Note.Remove(note);
            await _db.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/Notes/{id}/share
        // Compartir nota existente con otro usuario
        [HttpPost("{id}/share")]
        public async Task<IActionResult> ShareNote(Guid id, [FromQuery] Guid targetUserId, [FromQuery] string role = "read")
        {
            var note = await _db.Note.FindAsync(id);
            if (note == null)
                return NotFound("Nota no encontrada");

            // Solo el dueño original puede compartir
            if (note.UserId != CurrentUser.Id)
                return Forbid();

            // Evita duplicar permisos
            var existing = await _db.ResourceShares.FirstOrDefaultAsync(s =>
                s.ResourceId == id &&
                s.TargetUserId == targetUserId &&
                s.ResourceType == "Note");

            if (existing != null)
                return Conflict("El usuario ya tiene acceso a esta nota");

            var share = new SharedResource()
            {
                OwnerId = CurrentUser.Id,
                TargetUserId = targetUserId,
                ResourceId = id,
                ResourceType = "Note",
                Role = role
            };

            _db.ResourceShares.Add(share);
            await _db.SaveChangesAsync();

            return Ok($"Nota compartida con usuario {targetUserId} con rol {role}");
        }

        // GET: api/Notes/{id}/share
        // Ver usuarios con acceso compartido
        [HttpGet("{id}/share")]
        public async Task<ActionResult<IEnumerable<SharedResource>>> GetSharedUsers(Guid id)
        {
            var note = await _db.Note.FindAsync(id);
            if (note == null) return NotFound();

            if (note.UserId != CurrentUser.Id) return Forbid();

            return await _db.ResourceShares
                .Where(s => s.ResourceId == id && s.ResourceType == "Note")
                .ToListAsync();
        }

        // DELETE: api/Notes/{id}/share/{targetUserId}
        // Revocar acceso a usuario compartido
        [HttpDelete("{id}/share/{targetUserId}")]
        public async Task<IActionResult> RevokeShare(Guid id, Guid targetUserId)
        {
            var note = await _db.Note.FindAsync(id);
            if (note == null)
                return NotFound("Nota no encontrada");

            if (note.UserId != CurrentUser.Id)
                return Forbid();

            var share = await _db.ResourceShares.FirstOrDefaultAsync(s =>
                s.ResourceId == id &&
                s.TargetUserId == targetUserId &&
                s.ResourceType == "Note");

            if (share == null)
                return NotFound("No se encontró acceso compartido.");

            _db.ResourceShares.Remove(share);
            await _db.SaveChangesAsync();

            return Ok($"Acceso revocado para el usuario {targetUserId}");
        }

        // Verificar permisos
        
        private bool HasAccess(Note note, string required)
        {
            if (note.UserId == CurrentUser.Id)
                return true;

            var share = _db.ResourceShares.FirstOrDefault(s =>
                s.TargetUserId == CurrentUser.Id &&
                s.ResourceId == note.Id &&
                s.ResourceType == "Note");

            if (share == null)
                return false;

            return required switch
            {
                "read" => share.Role is "read" or "write" or "admin",
                "write" => share.Role is "write" or "admin",
                "admin" => share.Role == "admin",
                _ => false
            };
        }

    }
}
