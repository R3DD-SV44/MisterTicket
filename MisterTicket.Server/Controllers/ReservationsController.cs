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
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var reservation = await _context.Reservations
                .Include(r => r.SelectedSeats)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null) return NotFound("Réservation introuvable.");

            if (reservation.Status != ReservationStatus.OnGoing)
            {
                return BadRequest("Seules les réservations en cours peuvent être annulées.");
            }

            reservation.Status = ReservationStatus.Canceled;

            var seatIds = reservation.SelectedSeats.Select(s => s.Id).ToList();

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

            return Ok(new { message = "Réservation annulée et sièges libérés pour cet événement." });
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
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. On récupère la réservation avec ses sièges associés
            var res = await _context.Reservations
                .Include(r => r.SelectedSeats)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (res == null) return NotFound("Réservation introuvable.");
            if (res.Status != ReservationStatus.OnGoing)
                return BadRequest("Seules les réservations en cours peuvent être payées.");

            // 2. On change le statut de la réservation
            res.Status = ReservationStatus.Paid;

            // 3. On met à jour les entrées correspondantes dans EventSeats
            // On récupère les IDs des sièges de cette réservation
            var seatIds = res.SelectedSeats.Select(s => s.Id).ToList();

            // On cherche les EventSeats liés à CET événement pour CES sièges
            var eventSeats = await _context.EventSeats
                .Where(es => es.EventId == res.EventId && seatIds.Contains(es.SeatId))
                .ToListAsync();

            foreach (var es in eventSeats)
            {
                es.Status = SeatStatus.Paid;
                es.LockedUntil = null; // Libération définitive du verrou temporel
            }

            // 4. Sauvegarde et validation de la transaction
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // 5. Notification SignalR (On informe le front que ces sièges sont maintenant payés)
            foreach (var sId in seatIds)
            {
                await _hubContext.Clients.All.SendAsync("ReceiveSeatStatusUpdate", res.EventId, sId, SeatStatus.Paid);
            }

            return Ok(new
            {
                message = "Paiement fictif validé",
                pdfUrl = $"/api/tickets/{id}/pdf"
            });
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, "Une erreur est survenue lors du paiement.");
        }
    }
}