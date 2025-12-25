using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MisterTicket.Server.Data;
using MisterTicket.Server.Hubs;
using MisterTicket.Server.Models;
using System.Security.Claims;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ReservationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<TicketHub> _hubContext;

    public ReservationsController(ApplicationDbContext context, IHubContext<TicketHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    [HttpPost("confirm")]
    public async Task<IActionResult> CreateReservation([FromBody] List<int> seatIds)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var seats = await _context.Seats.Where(s => seatIds.Contains(s.Id)).ToListAsync();

        var reservation = new Reservation
        {
            UserId = userId!,
            SelectedSeats = seats,
            Status = ReservationStatus.EnCours,
            ReservationDate = DateTime.UtcNow
        };

        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();

        return Ok(new { reservationId = reservation.Id, total = seats.Sum(s => s.Price) });
    }

    [HttpPost("{id}/pay")]
    public async Task<IActionResult> Pay(int id)
    {
        var res = await _context.Reservations.Include(r => r.SelectedSeats).FirstOrDefaultAsync(r => r.Id == id);
        if (res == null) return NotFound();

        res.Status = ReservationStatus.Payee;
        foreach (var seat in res.SelectedSeats)
        {
            seat.Status = SeatStatus.Paid; // Passage à l'état "payé" [cite: 25, 45]
        }

        await _context.SaveChangesAsync();

        await _hubContext.Clients.All.SendAsync("UpdateSeats", res.SelectedSeats.Select(s => s.Id));

        return Ok(new { message = "Paiement fictif validé", pdfUrl = $"/api/tickets/{id}/pdf" });
    }
}