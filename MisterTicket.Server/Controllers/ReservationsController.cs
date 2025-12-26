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
            Status = ReservationStatus.OnGoing,
            ReservationDate = DateTime.UtcNow
        };

        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();

        return Ok(new { reservationId = reservation.Id, total = seats.Sum(s => s.Price) });
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelReservation(int id)
    {
        var reservation = await _context.Reservations
            .Include(r => r.SelectedSeats)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation == null) return NotFound();

        if (reservation.Status != ReservationStatus.OnGoing)
        {
            return BadRequest("Seules les réservations en cours peuvent être annulées.");
        }

        reservation.Status = ReservationStatus.Canceled;

        foreach (var seat in reservation.SelectedSeats)
        {
            seat.Status = SeatStatus.Free;
            seat.LockedUntil = null;
            await _hubContext.Clients.All.SendAsync("ReceiveSeatStatusUpdate", seat.Id, SeatStatus.Free);
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Réservation annulée et sièges libérés." });
    }

    [HttpPost("{id}/pay")]
    public async Task<IActionResult> Pay(int id)
    {
        var res = await _context.Reservations.Include(r => r.SelectedSeats).FirstOrDefaultAsync(r => r.Id == id);
        if (res == null) return NotFound();

        res.Status = ReservationStatus.Paid;
        foreach (var seat in res.SelectedSeats)
        {
            seat.Status = SeatStatus.Paid; 
        }

        await _context.SaveChangesAsync();

        await _hubContext.Clients.All.SendAsync("UpdateSeats", res.SelectedSeats.Select(s => s.Id));

        return Ok(new { message = "Paiement fictif validé", pdfUrl = $"/api/tickets/{id}/pdf" });
    }
}