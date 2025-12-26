using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MisterTicket.Server.Data;
using MisterTicket.Server.Models;

[Authorize(Policy = "Organiser")]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("event-stats/{eventId}")]
    public async Task<IActionResult> GetEventStats(int eventId)
    {
        var ev = await _context.Events
            .Include(e => e.Scene)
            .ThenInclude(s => s.Seats)
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (ev == null) return NotFound("Événement non trouvé.");

        var allSeats = ev.Scene?.Seats ?? new List<Seat>();

        int totalCapacity = ev.Scene?.Capacity ?? 0;
        int seatsPaid = allSeats.Count(s => s.Status == SeatStatus.Paid);
        int seatsReserved = allSeats.Count(s => s.Status == SeatStatus.ReservedTemp);
        int seatsFree = allSeats.Count(s => s.Status == SeatStatus.Free);

        decimal totalRevenue = allSeats
            .Where(s => s.Status == SeatStatus.Paid)
            .Sum(s => s.Price);

        double fillingRate = totalCapacity > 0
            ? Math.Round((double)seatsPaid / totalCapacity * 100, 2)
            : 0;

        return Ok(new
        {
            EventName = ev.Name,
            TotalCapacity = totalCapacity,
            Stats = new
            {
                Paid = seatsPaid,
                Reserved = seatsReserved,
                Free = seatsFree,
                FillingRate = fillingRate
            },
            TotalRevenue = totalRevenue
        });
    }
}