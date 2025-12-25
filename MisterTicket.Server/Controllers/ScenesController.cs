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