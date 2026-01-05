using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MisterTicket.Server.Data;
using MisterTicket.Server.Models;

[Authorize(Policy = "Management")]
[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("{eventId}_event_stats")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEventStats(int eventId)
    {
        var stats = await _context.EventSeats
            .Where(es => es.EventId == eventId)
            .ToListAsync();

        if (!stats.Any()) return NotFound("Aucune donnée pour cet événement.");

        var realRevenue = await _context.Payments
            .Include(p => p.Reservation)
            .Where(p => p.Reservation.EventId == eventId && p.Status == PaymentStatus.Success)
            .SumAsync(p => p.Value);

        var paidSeatsCount = stats.Count(s => s.Status == SeatStatus.Paid);

        return Ok(new
        {
            Paid = paidSeatsCount,
            Reserved = stats.Count(s => s.Status == SeatStatus.ReservedTemp),
            Free = stats.Count(s => s.Status == SeatStatus.Free),
            Revenue = realRevenue,
            FillingRate = stats.Count > 0
                ? Math.Round((double)paidSeatsCount / stats.Count * 100, 2)
                : 0
        });
    }
}