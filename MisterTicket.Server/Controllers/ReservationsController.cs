using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MisterTicket.Server.Data;
using MisterTicket.Server.Hubs;
using MisterTicket.Server.Models;
using System.Security.Claims;

namespace MisterTicket.Server.Controllers;

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
    public async Task<IActionResult> CreateReservation([FromBody] ConfirmReservationRequest request)
    {
        // 1. Récupération et conversion de l'ID utilisateur
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out int userId)) return Unauthorized();

        // 2. Récupération des sièges avec leur zone tarifaire pour le prix
        var seats = await _context.Seats
            .Include(s => s.PriceZone)
            .Where(s => request.SeatIds.Contains(s.Id))
            .ToListAsync();

        if (!seats.Any()) return BadRequest("Aucun siège sélectionné.");

        // 3. Création de la réservation avec l'EventId
        var reservation = new Reservation
        {
            UserId = userId,
            EventId = request.EventId,
            SelectedSeats = seats,
            Status = ReservationStatus.OnGoing,
            ReservationDate = DateTime.UtcNow
        };

        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();

        // Calcul du total via les zones tarifaires
        var total = seats.Sum(s => s.PriceZone?.Price ?? 0);

        return Ok(new { reservationId = reservation.Id, total });
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