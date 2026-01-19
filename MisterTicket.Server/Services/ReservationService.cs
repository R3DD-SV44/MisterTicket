using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MisterTicket.Server.Data;
using MisterTicket.Server.DTOs;
using MisterTicket.Server.Hubs;
using MisterTicket.Server.Models;

namespace MisterTicket.Server.Services;

public class ReservationService : IReservationService
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<TicketHub> _hubContext;

    public ReservationService(ApplicationDbContext context, IHubContext<TicketHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    public async Task<(bool Success, Reservation Reservation, string Error, int HttpStatusCode)> CreateReservationAsync(int userId, ReservationDto request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var eventExists = await _context.Events.AnyAsync(e => e.Id == request.EventId);
            if (!eventExists)
            {
                return (false, null, $"L'événement {request.EventId} n'existe pas.", 404);
            }

            // Récupérer les sièges
            var eventSeats = await _context.EventSeats
                .Where(es => es.EventId == request.EventId && request.SeatIds.Contains(es.SeatId))
                .ToListAsync();

            if (eventSeats.Count != request.SeatIds.Count)
            {
                return (false, null, "Certains sièges sont invalides.", 400);
            }

            // Vérifier si déjà pris
            foreach (var es in eventSeats)
            {
                bool isLocked = es.Status == SeatStatus.Paid ||
                               (es.Status == SeatStatus.ReservedTemp && es.LockedUntil > DateTime.UtcNow);

                if (isLocked)
                {
                    return (false, null, $"Le siège {es.SeatId} est déjà réservé.", 409);
                }
            }

            // Verrouiller les sièges
            foreach (var es in eventSeats)
            {
                es.Status = SeatStatus.ReservedTemp;
                es.LockedUntil = DateTime.UtcNow.AddMinutes(15);
                es.ReservedByUserId = userId;

                // Notification SignalR
                await _hubContext.Clients.All.SendAsync("ReceiveSeatStatusUpdate", es.EventId, es.SeatId, SeatStatus.ReservedTemp);
            }

            // Créer la réservation
            var seats = await _context.Seats.Where(s => request.SeatIds.Contains(s.Id)).ToListAsync();

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
            await transaction.CommitAsync();

            return (true, reservation, null, 201);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            // Idéalement, on log l'erreur ici : _logger.LogError(ex, "Erreur réservation");
            return (false, null, "Une erreur interne est survenue.", 500);
        }
    }
}