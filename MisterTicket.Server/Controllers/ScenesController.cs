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
    public ScenesController(ApplicationDbContext context) => _context = context;

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateScene(SceneDto dto)
    {
        // 1. Vérification : une scène avec le même nom existe-t-elle déjà ?
        var sceneExists = await _context.Scenes.AnyAsync(s => s.Name == dto.Name);
        if (sceneExists)
        {
            return BadRequest(new { message = $"A scene with the name '{dto.Name}' already exists." });
        }

        // 2. Conversion du DTO en modèle de données (Entity)
        var scene = new Scene
        {
            Name = dto.Name,
            MaxRows = dto.MaxRows,
            MaxColumns = dto.MaxColumns
        };

        // 3. Ajout et sauvegarde
        _context.Scenes.Add(scene);
        await _context.SaveChangesAsync();

        // 4. Mise à jour du DTO avec l'ID généré
        dto.Id = scene.Id;

        // 5. Retourne un code 201 Created
        // Note : Assurez-vous d'avoir une méthode GetSceneById avec [HttpGet("{id}")]
        // Si vous utilisez GetLayout, remplacez "GetSceneById" par "GetLayout"
        return CreatedAtAction("GetSceneById", new { id = scene.Id }, dto);
    }


    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ModifyScene(int id, SceneDto dto)
    {
        if (id != dto.Id)
        {
            return BadRequest(new { message = "ID mismatch between URL and body." });
        }

        var scene = await _context.Scenes.FindAsync(id);

        if (scene == null)
        {
            return NotFound(new { message = $"Scene with ID {id} not found." });
        }

        if (scene.Name != dto.Name && await _context.Scenes.AnyAsync(s => s.Name == dto.Name))
        {
            return BadRequest(new { message = $"A scene with the name '{dto.Name}' already exists." });
        }

        scene.Name = dto.Name;
        scene.MaxRows = dto.MaxRows;
        scene.MaxColumns = dto.MaxColumns;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Scenes.AnyAsync(s => s.Id == id))
            {
                return NotFound(new { message = "The scene no longer exists." });
            }
            throw;
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteScene(int id)
    {
        var scene = await _context.Scenes.FindAsync(id);

        if (scene == null)
        {
            return NotFound(new { message = $"Scene with ID {id} not found." });
        }

        var hasEvents = await _context.Events.AnyAsync(e => e.SceneId == id);
        if (hasEvents)
        {
            return BadRequest(new { message = "Cannot delete scene: it is associated with existing events." });
        }

        var hasSeats = await _context.Seats.AnyAsync(s => s.SceneId == id);
        if (hasSeats)
        {
            _context.Seats.RemoveRange(scene.Seats);
        }

        var hasPriceZones = await _context.PriceZones.AnyAsync(pz => pz.SceneId == id);
        if (hasPriceZones)
        {
            return BadRequest(new { message = "Cannot delete scene: it contains price zones. Please delete them first." });
        }

        _context.Scenes.Remove(scene);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return BadRequest(new { message = "An error occurred while deleting the scene from the database." });
        }

        return NoContent();
    }

    [HttpGet("{id}")]
    [AllowAnonymous] // Permet de voir la scène sans être connecté
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Scene>> GetSceneById(int id)
    {
        var scene = await _context.Scenes.FindAsync(id);

        if (scene == null)
        {
            return NotFound(new { message = $"Scene with ID {id} not found." });
        }

        return Ok(scene);
    }

    [HttpGet("{id}/layout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLayout(int id)
    {
        // 1. Récupération de la scène avec ses dépendances
        var scene = await _context.Scenes
            .Include(s => s.Seats)
            .Include(s => s.PriceZones)
            .FirstOrDefaultAsync(s => s.Id == id);

        // 2. Vérification d'existence
        if (scene == null)
        {
            return NotFound(new { message = $"Scene with ID {id} not found." });
        }

        // 3. Retourne les données (200 OK)
        return Ok(scene);
    }
}