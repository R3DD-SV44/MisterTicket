using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MisterTicket.Server.Data;
using MisterTicket.Server.Models;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    public EventsController(ApplicationDbContext context) => _context = context;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Event>>> GetEvents()
    {
        return await _context.Events.Include(e => e.Scene).ToListAsync();
    }

    [Authorize(Policy = "Organiser")]
    [HttpPost]
    public async Task<ActionResult<Event>> PostEvent(Event @event)
    {
        _context.Events.Add(@event);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetEvents), new { id = @event.Id }, @event);
    }
    // Dans Controllers/EventsController.cs

    [Authorize(Policy = "Organiser")]
    [HttpPut("{id}")]
    public async Task<IActionResult> ModifyEvent(int id, Event @event)
    {
        if (id != @event.Id) return BadRequest();

        _context.Entry(@event).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Events.Any(e => e.Id == id)) return NotFound();
            else throw; 
        }

        return NoContent();
    }

    [Authorize(Policy = "Organiser")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var @event = await _context.Events.FindAsync(id);
        if (@event == null) return NotFound();

        _context.Events.Remove(@event);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}