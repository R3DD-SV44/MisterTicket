using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MisterTicket.Server.Data;
using MisterTicket.Server.Hubs;
using MisterTicket.Server.Models;
using MisterTicket.Server.DTOs;
using MisterTicket.Server.Services;
using System.Security.Claims;

namespace MisterTicket.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ReservationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<TicketHub> _hubContext;
    private readonly IReservationService _reservationService;

    public ReservationsController(
        ApplicationDbContext context,
        IHubContext<TicketHub> hubContext,
        IReservationService reservationService)
    {
        _context = context;
        _hubContext = hubContext;
        _reservationService = reservationService;
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReservationById(int id)
    {
        var reservation = await _context.Reservations
            .Include(r => r.SelectedSeats)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation == null)
        {
            return NotFound();
        }

        return Ok(reservation);
    }

    [HttpGet("my-reservations")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyReservations()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new { message = "User is not authenticated." });
        }

        var reservations = await _context.Reservations
            .Include(r => r.Event)
            .Include(r => r.SelectedSeats)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.ReservationDate)
            .ToListAsync();

        return Ok(reservations);
    }

    [HttpPost("confirm")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateReservation([FromBody] ReservationDto request)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out int userId))
        {
            return Unauthorized(new { message = "User is not authenticated." });
        }

        var result = await _reservationService.CreateReservationAsync(userId, request);

        if (!result.Success)
        {
            return StatusCode(result.HttpStatusCode, new { message = result.Error });
        }

        var reservation = result.Reservation;
        var total = reservation.SelectedSeats.Sum(s => s.Price);
        request.Id = reservation.Id;

        return CreatedAtAction("GetReservationById", new { id = reservation.Id }, new
        {
            reservationId = reservation.Id,
            total = total,
            details = request
        });
    }

    [HttpPost("{id}/pay")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Pay(int id)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new { message = "User is not authenticated." });
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var res = await _context.Reservations
                .Include(r => r.SelectedSeats)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (res == null)
            {
                return NotFound(new { message = $"Reservation with ID {id} not found." });
            }

            var eventExists = await _context.Events.AnyAsync(e => e.Id == res.EventId);
            if (!eventExists)
            {
                return NotFound(new { message = $"The event associated with this reservation (ID: {res.EventId}) no longer exists." });
            }

            if (res.UserId != userId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not have permission to pay for this reservation." });
            }

            if (res.Status != ReservationStatus.OnGoing)
            {
                return BadRequest(new { message = "Only ongoing reservations can be paid." });
            }

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

            var payment = new Payment
            {
                Reference = $"PAY-{Guid.NewGuid().ToString().Substring(0, 8)}",
                Value = res.SelectedSeats.Sum(s => s.Price),
                Status = PaymentStatus.Success,
                ReservationId = res.Id
            };
            _context.Payments.Add(payment);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                message = "Fictitious payment validated.",
                pdfUrl = $"/api/tickets/{id}/pdf"
            });
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred during the payment process." });
        }
    }

    [HttpPost("{id}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CancelReservation(int id)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new { message = "User is not authenticated." });
        }

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var reservation = await _context.Reservations
                .Include(r => r.SelectedSeats)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reservation == null)
            {
                return NotFound(new { message = $"Reservation with ID {id} not found." });
            }

            var eventExists = await _context.Events.AnyAsync(e => e.Id == reservation.EventId);
            if (!eventExists)
            {
                return NotFound(new { message = $"The event associated with this reservation (ID: {reservation.EventId}) no longer exists." });
            }

            if (reservation.UserId != userId)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not have permission to cancel this reservation." });
            }

            if (reservation.Status != ReservationStatus.OnGoing)
            {
                return BadRequest(new { message = "Only ongoing reservations can be canceled." });
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

            return Ok(new { message = "Reservation canceled and seats released." });
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred during cancellation." });
        }
    }
}