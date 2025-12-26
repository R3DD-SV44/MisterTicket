using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MisterTicket.Server.Data;
using MisterTicket.Server.Hubs;
using MisterTicket.Server.Models;
using System.Security.Claims;

namespace MisterTicket.Server.Controllers;

public class ReservationRequest
{     public int EventId { get; set; }
    public List<int> SeatIds { get; set; } = new();
}
// Objet pour recevoir les données de confirmation
public record ConfirmReservationRequest(int EventId, List<int> SeatIds);

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
    public async Task<IActionResult> CreateReservation([FromBody] ReservationRequest request)
    {
        // Récupération et conversion de l'ID utilisateur
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

        var seats = await _context.Seats.Where(s => request.SeatIds.Contains(s.Id)).ToListAsync();

        var reservation = new Reservation
        {
            UserId = userId, // Utilise maintenant l'int
            EventId = request.EventId, // Définit l'ID de l'événement
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
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out int userId)) return Unauthorized();

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var reservation = await _context.Reservations
                .Include(r => r.SelectedSeats)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null) return NotFound("Réservation introuvable.");

            // Sécurité : Seul le propriétaire peut annuler
            if (reservation.UserId != userId) return Forbid();

            if (reservation.Status != ReservationStatus.OnGoing)
            {
                return BadRequest("Seules les réservations en cours peuvent être annulées.");
            }

            reservation.Status = ReservationStatus.Canceled;

            var seatIds = reservation.SelectedSeats.Select(s => s.Id).ToList();

            // Mise à jour de la table de liaison EventSeats
            var eventSeats = await _context.EventSeats
                .Where(es => es.EventId == reservation.EventId && seatIds.Contains(es.SeatId))
                .ToListAsync();

            foreach (var es in eventSeats)
            {
                es.Status = SeatStatus.Free;
                es.LockedUntil = null;
                es.ReservedByUserId = null;

                await _hubContext.Clients.All.SendAsync("ReceiveSeatStatusUpdate", es.EventId, es.SeatId, SeatStatus.Free);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { message = "Réservation annulée et sièges libérés." });
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, "Erreur lors de l'annulation.");
        }
    }

    [HttpPost("{id}/pay")]
    public async Task<IActionResult> Pay(int id)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out int userId)) return Unauthorized();

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var res = await _context.Reservations
                .Include(r => r.SelectedSeats)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (res == null) return NotFound("Réservation introuvable.");

            if (res.UserId != userId) return Forbid();

            if (res.Status != ReservationStatus.OnGoing)
                return BadRequest("Seules les réservations en cours peuvent être payées.");

            res.Status = ReservationStatus.Paid;

            var seatIds = res.SelectedSeats.Select(s => s.Id).ToList();

            var eventSeats = await _context.EventSeats
                .Where(es => es.EventId == res.EventId && seatIds.Contains(es.SeatId))
                .ToListAsync();

            foreach (var es in eventSeats)
            {
                es.Status = SeatStatus.Paid;
                es.LockedUntil = null;

                await _hubContext.Clients.All.SendAsync("ReceiveSeatStatusUpdate", res.EventId, es.SeatId, SeatStatus.Paid);
            }

            await _context.SaveChangesAsync();

            var payment = new Payment
            {
                Reference = $"PAY-{Guid.NewGuid().ToString().Substring(0, 8)}",
                Value = res.SelectedSeats.Sum(s => s.Price),
                Status = PaymentStatus.Success,
                ReservationId = res.Id
            };
            _context.Payments.Add(payment);

            await transaction.CommitAsync();

            return Ok(new
            {
                message = "Paiement fictif validé",
                pdfUrl = $"/api/tickets/{id}/pdf"
            });
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, "Erreur lors du paiement.");
        }
    }
}