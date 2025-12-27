using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MisterTicket.Server.Data;
using MisterTicket.Server.Models;
using MisterTicket.Server.DTOs;

[ApiController]
[Route("api/[controller]")]
public class EventsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    public EventsController(ApplicationDbContext context) => _context = context;

    [HttpGet("getAll")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<EventDto>>> GetEvents()
    {
        if (!await _context.Events.AnyAsync())
        {
            return NotFound(new { message = "No Event has been created" });
        }

        var query = _context.Events.AsQueryable();

        var events = await query
            .Select(e => new EventDto
            {
                Id = e.Id,
                Name = e.Name,
                Description = e.Description,
                Date = e.Date,
                SceneId = e.SceneId
            })
            .ToListAsync();

        return Ok(events);
    }



    [HttpGet("get_{id}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<ActionResult<EventDto>> GetEventById(int id)
    {
        var @event = await _context.Events
            .Where(e => e.Id == id)
            .Select(e => new EventDto
            {
                Id = e.Id,
                Name = e.Name,
                Description = e.Description,
                Date = e.Date,
                SceneId = e.SceneId
            })
            .FirstOrDefaultAsync();

        if (@event == null)
        {
            return NotFound(new { message = $"Event with ID {id} not found" });
        }

        return Ok(@event);
    }


    [ProducesResponseType(StatusCodes.Status201Created)] // Succès
    [ProducesResponseType(StatusCodes.Status404NotFound)] // Scène introuvable
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Authorize(Policy = "Organiser")]
    [HttpPost("create")]
    public async Task<ActionResult<Event>> CreateEvent(EventDto dto)
    {
        var sceneExists = await _context.Scene.AnyAsync(s => s.Id == dto.SceneId);

        if (!sceneExists)
        {
            return NotFound(new { message = $"This Scene (ID: {dto.SceneId}) does not exist. Please create the scene first." });
        }

        var @event = new Event
        {
            Name = dto.Name,
            Description = dto.Description,
            Date = dto.Date,
            SceneId = dto.SceneId
        };

        _context.Events.Add(@event);
        await _context.SaveChangesAsync();
        dto.Id = @event.Id;
        return CreatedAtAction(nameof(GetEventById), new { id = @event.Id }, dto);
    }


    [Authorize(Policy = "Organiser")]
    [HttpPut("modify_{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)] // Succès (Pas de contenu à renvoyer)
    [ProducesResponseType(StatusCodes.Status404NotFound)] // Événement ou Scène introuvable
    [ProducesResponseType(StatusCodes.Status400BadRequest)] // ID incohérent
    public async Task<IActionResult> ModifyEvent(int id, EventDto dto)
    {
        // 1. Vérification de l'existence de l'événement
        var @event = await _context.Events.FindAsync(id);
        if (@event == null)
        {
            return NotFound(new { message = $"Event with ID {id} not found." });
        }

        // 2. Vérification de l'existence de la scène
        // On utilise 'Scene' car c'est le nom dans votre ApplicationDbContext
        var sceneExists = await _context.Scene.AnyAsync(s => s.Id == dto.SceneId);
        if (!sceneExists)
        {
            return NotFound(new { message = $"Scene with ID {dto.SceneId} does not exist. Modification aborted." });
        }

        // 3. Mise à jour des champs de l'entité existante avec les données du DTO
        @event.Name = dto.Name;
        @event.Description = dto.Description;
        @event.Date = dto.Date;
        @event.SceneId = dto.SceneId;

        // 4. Enregistrement des modifications
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Events.Any(e => e.Id == id)) return NotFound();
            else throw;
        }

        // 5. Pour un PUT (modification), on renvoie généralement un 204 No Content
        return Ok();
    }


    [Authorize(Policy = "Organiser")]
    [HttpDelete("delete_{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)] // Succès
    [ProducesResponseType(StatusCodes.Status404NotFound)] // Événement ou Scène introuvable
    public async Task<IActionResult> DeleteEvent(int id)
    {
        // 1. Vérification de l'existence de l'événement
        var @event = await _context.Events.FindAsync(id);
        if (@event == null)
        {
            return NotFound(new { message = $"Event with ID {id} not found." });
        }

        // 2. Vérification de l'existence de la scène associée
        // On vérifie si la scène liée à cet événement existe toujours en base
        var sceneExists = await _context.Scene.AnyAsync(s => s.Id == @event.SceneId);
        if (!sceneExists)
        {
            return NotFound(new { message = $"The scene associated with this event (ID: {@event.SceneId}) does not exist." });
        }

        // 3. Suppression de l'événement
        _context.Events.Remove(@event);
        await _context.SaveChangesAsync();

        // 4. Retourne 204 No Content pour confirmer la suppression réussie
        return Ok();
    }
}