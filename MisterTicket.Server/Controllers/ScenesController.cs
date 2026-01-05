using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MisterTicket.Server.Data;
using MisterTicket.Server.DTOs;
using MisterTicket.Server.Models;

[Authorize(Roles = "Admin,Organiser")]
[ApiController]
[Route("api/[controller]")]
public class ScenesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public ScenesController(ApplicationDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SceneDto>>> GetScenes()
    {
        return await _context.Scenes
            .Select(s => new SceneDto
            {
                Id = s.Id,
                Name = s.Name,
                MaxRows = s.MaxRows,
                MaxColumns = s.MaxColumns,
                ImageUrl = s.ImageUrl
            }).ToListAsync();
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Scene>> GetSceneById(int id)
    {
        var scene = await _context.Scenes.FindAsync(id);
        return scene == null ? NotFound() : Ok(scene);
    }

    [HttpGet("{id}/layout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLayout(int id)
    {
        var scene = await _context.Scenes
            .Include(s => s.Seats)
            .Include(s => s.PriceZones)
            .FirstOrDefaultAsync(s => s.Id == id);

        return scene == null ? NotFound() : Ok(scene);
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateScene([FromForm] SceneDto dto)
    {
        var sceneExists = await _context.Scenes.AnyAsync(s => s.Name == dto.Name);
        if (sceneExists)
        {
            return BadRequest(new { message = $"A scene with the name '{dto.Name}' already exists." });
        }

        var scene = new Scene
        {
            Name = dto.Name,
            MaxRows = dto.MaxRows,
            MaxColumns = dto.MaxColumns
        };

        if (dto.ImageFile != null)
        {
            scene.ImageUrl = await SaveImage(dto.ImageFile);
        }

        _context.Scenes.Add(scene);
        await _context.SaveChangesAsync();

        dto.Id = scene.Id;
        return CreatedAtAction("GetSceneById", new { id = scene.Id }, dto);
    }

    [HttpPost("{id}/seats/update-prices")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSeatPrices(int id, [FromBody] List<SeatUpdateDto> updates)
    {
        var scene = await _context.Scenes
            .Include(s => s.Seats)
            .Include(s => s.PriceZones)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (scene == null) return NotFound();

        foreach (var update in updates)
        {
            var seat = scene.Seats.FirstOrDefault(s => s.Row == update.Row && s.Column == update.Column);

            if (update.PriceZoneId == null)
            {
                if (seat != null) _context.Seats.Remove(seat);
            }
            else
            {
                var zone = scene.PriceZones.FirstOrDefault(z => z.Id == update.PriceZoneId);
                if (zone == null) continue;

                if (seat != null)
                {
                    seat.PriceZoneId = update.PriceZoneId.Value;
                    seat.Price = zone.Price;
                }
                else
                {
                    _context.Seats.Add(new Seat
                    {
                        SceneId = id,
                        Row = update.Row,
                        Column = update.Column,
                        PriceZoneId = update.PriceZoneId.Value,
                        Price = zone.Price,
                        Number = $"R{update.Row}C{update.Column}"
                    });
                }
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Plan de salle mis à jour." });
    }

    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ModifyScene(int id, [FromForm] SceneDto dto)
    {
        if (id != dto.Id) return BadRequest(new { message = "ID mismatch." });

        var scene = await _context.Scenes.FindAsync(id);
        if (scene == null) return NotFound();

        if (scene.Name != dto.Name && await _context.Scenes.AnyAsync(s => s.Name == dto.Name))
        {
            return BadRequest(new { message = "Name already exists." });
        }

        scene.Name = dto.Name;
        scene.MaxRows = dto.MaxRows;
        scene.MaxColumns = dto.MaxColumns;

        if (dto.ImageFile != null)
        {
            if (!string.IsNullOrEmpty(scene.ImageUrl))
            {
                var oldPath = Path.Combine(_environment.WebRootPath, scene.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
            }
            scene.ImageUrl = await SaveImage(dto.ImageFile);
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteScene(int id)
    {
        var scene = await _context.Scenes.Include(s => s.Seats).FirstOrDefaultAsync(s => s.Id == id);
        if (scene == null) return NotFound();

        if (await _context.Events.AnyAsync(e => e.SceneId == id))
        {
            return BadRequest(new { message = "Cannot delete: associated events exist." });
        }

        if (scene.Seats.Any())
        {
            _context.Seats.RemoveRange(scene.Seats);
        }

        _context.Scenes.Remove(scene);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private async Task<string> SaveImage(IFormFile file)
    {
        var folderPath = Path.Combine(_environment.WebRootPath, "uploads");
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
        var filePath = Path.Combine(folderPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }
        return "/uploads/" + fileName;
    }
}