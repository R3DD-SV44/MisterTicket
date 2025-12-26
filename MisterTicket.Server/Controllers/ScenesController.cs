using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MisterTicket.Server.Data;
using MisterTicket.Server.Models;

[Authorize(Roles = "Admin,Organiser")]
[ApiController]
[Route("api/[controller]")]
public class ScenesController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    public ScenesController(ApplicationDbContext context) => _context = context;

    [HttpPost]
    public async Task<IActionResult> CreateScene(Scene scene)
    {
        _context.Stadia.Add(scene);
        await _context.SaveChangesAsync();
        return Ok(scene);
    }

    // Dans Controllers/ScenesController.cs

    [HttpPut("{id}")]
    public async Task<IActionResult> ModifyScene(int id, Scene scene)
    {
        if (id != scene.Id) return BadRequest();

        _context.Entry(scene).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!_context.Stadia.Any(s => s.Id == id)) return NotFound();
            else throw;
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteScene(int id)
    {
        var scene = await _context.Stadia.FindAsync(id);
        if (scene == null) return NotFound();

        // Note: Vérifiez si des événements sont liés à cette scène avant de supprimer
        var hasEvents = await _context.Events.AnyAsync(e => e.SceneId == id);
        if (hasEvents) return BadRequest("Impossible de supprimer une scène associée à des événements.");

        _context.Stadia.Remove(scene);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpGet("{id}/layout")]
    [AllowAnonymous] // Tout le monde peut voir la carte interactive [cite: 5, 43]
    public async Task<IActionResult> GetLayout(int id)
    {
        var stadium = await _context.Stadia
            .Include(s => s.Seats)
            .Include(s => s.PriceZones)
            .FirstOrDefaultAsync(s => s.Id == id);
        return Ok(stadium);
    }
}