using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MisterTicket.Server.Data;
using MisterTicket.Server.Models;

[Authorize(Policy = "Management")]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [HttpGet("{eventId}_event_stats")]
    public async Task<IActionResult> GetEventStats(int eventId)
    {
        var stats = await _context.EventSeats
            .Where(es => es.EventId == eventId)
            .Include(es => es.Seat)
                .ThenInclude(s => s.PriceZone)
            .ToListAsync();

        if (!stats.Any()) return NotFound("Aucune donnée pour cet événement.");

        var paidSeats = stats.Where(s => s.Status == SeatStatus.Paid).ToList();

        return Ok(new
        {
            Paid = paidSeats.Count,
            Reserved = stats.Count(s => s.Status == SeatStatus.ReservedTemp),
            Free = stats.Count(s => s.Status == SeatStatus.Free),
            Revenue = paidSeats.Sum(s => s.Seat?.PriceZone?.Price ?? 0),
            FillingRate = Math.Round((double)paidSeats.Count / stats.Count * 100, 2)
        });
    }
}