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
    private readonly IWebHostEnvironment _environment;
    private readonly ApplicationDbContext _context;

    public EventsController(ApplicationDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    [HttpGet("getAll")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<EventDto>>> GetEvents()
    {
        var events = await _context.Events
            .Include(e => e.Scene)
            .Select(e => new EventDto
            {
                Id = e.Id,
                Name = e.Name,
                Description = e.Description,
                Date = e.Date,
                SceneId = e.SceneId,
                ImageUrl = e.ImageUrl,
                SceneName = e.Scene != null ? e.Scene.Name : "Salle inconnue"
            })
            .ToListAsync();

        if (events.Count == 0) return NotFound(new { message = "Aucun événement" });

        return Ok(events);
    }

    [HttpGet("get_{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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
                SceneId = e.SceneId,
                ImageUrl = e.ImageUrl,
                SceneName = e.Scene != null ? e.Scene.Name : "Lieu inconnu"
            })
            .FirstOrDefaultAsync();

        if (@event == null)
        {
            return NotFound(new { message = $"Event with ID {id} not found" });
        }

        return Ok(@event);
    }

    [HttpGet("{id}/seats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEventSeats(int id)
    {
        var seats = await _context.EventSeats
            .Where(es => es.EventId == id && es.Status != SeatStatus.Free)
            .Select(es => new { es.SeatId, es.Status })
            .ToListAsync();

        return Ok(seats);
    }

    [Authorize(Policy = "Management")]
    [HttpPost("create")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Event>> CreateEvent([FromForm] EventDto dto)
    {
        var sceneExists = await _context.Scenes.AnyAsync(s => s.Id == dto.SceneId);
        if (!sceneExists)
        {
            return NotFound(new { message = $"This Scene (ID: {dto.SceneId}) does not exist." });
        }

        var @event = new Event
        {
            Name = dto.Name,
            Description = dto.Description,
            Date = dto.Date,
            SceneId = dto.SceneId
        };

        if (dto.ImageFile != null)
        {
            var folderPath = Path.Combine(_environment.WebRootPath, "uploads");
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(dto.ImageFile.FileName);
            var filePath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await dto.ImageFile.CopyToAsync(stream);
            }
            @event.ImageUrl = "/uploads/" + fileName;
        }

        _context.Events.Add(@event);

        var seatsInScene = await _context.Seats
            .Where(s => s.SceneId == dto.SceneId)
            .ToListAsync();

        if (seatsInScene.Any())
        {
            var eventSeats = seatsInScene.Select(s => new EventSeat
            {
                Event = @event,
                SeatId = s.Id,
                Status = SeatStatus.Free,
                LockedUntil = null,
                ReservedByUserId = null
            });

            _context.EventSeats.AddRange(eventSeats);
        }

        await _context.SaveChangesAsync();

        dto.Id = @event.Id;
        return CreatedAtAction(nameof(GetEventById), new { id = @event.Id }, dto);
    }

    [Authorize(Policy = "Management")]
    [HttpPut("modify_{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ModifyEvent(int id, [FromForm] EventDto dto)
    {
        var @event = await _context.Events.FindAsync(id);
        if (@event == null) return NotFound();

        var sceneExists = await _context.Scenes.AnyAsync(s => s.Id == dto.SceneId);
        if (!sceneExists) return NotFound(new { message = "Scène introuvable" });

        @event.Name = dto.Name;
        @event.Description = dto.Description;
        @event.Date = dto.Date;
        @event.SceneId = dto.SceneId;

        if (dto.ImageFile != null)
        {
            if (!string.IsNullOrEmpty(@event.ImageUrl))
            {
                var oldPath = Path.Combine(_environment.WebRootPath, @event.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
            }

            var folderPath = Path.Combine(_environment.WebRootPath, "uploads");
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(dto.ImageFile.FileName);
            var filePath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await dto.ImageFile.CopyToAsync(stream);
            }
            @event.ImageUrl = "/uploads/" + fileName;
        }

        await _context.SaveChangesAsync();
        return Ok();
    }

    [Authorize(Policy = "Management")]
    [HttpDelete("delete_{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var @event = await _context.Events.FindAsync(id);
        if (@event == null)
        {
            return NotFound(new { message = $"Event with ID {id} not found." });
        }

        var reservations = await _context.Reservations
            .Include(r => r.SelectedSeats)
            .Where(r => r.EventId == id)
            .ToListAsync();

        foreach (var reservation in reservations)
        {
            reservation.SelectedSeats.Clear();
        }

        _context.Reservations.RemoveRange(reservations);

        var eventSeats = await _context.EventSeats
            .Where(es => es.EventId == id)
            .ToListAsync();
        _context.EventSeats.RemoveRange(eventSeats);

        _context.Events.Remove(@event);

        await _context.SaveChangesAsync();

        return Ok();
    }
}