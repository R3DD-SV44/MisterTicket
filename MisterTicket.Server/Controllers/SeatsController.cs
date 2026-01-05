using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MisterTicket.Server.Data;
using MisterTicket.Server.Hubs;
using MisterTicket.Server.Models;
using System.Security.Claims;

namespace MisterTicket.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SeatsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<TicketHub> _hubContext;

        public SeatsController(ApplicationDbContext context, IHubContext<TicketHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        [HttpPost("lock/event_{eventId}/seat_{seatId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> LockSeat(int eventId, int seatId)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { message = "User must be authenticated to lock a seat." });
            }

            var eventSeat = await _context.EventSeats
                .FirstOrDefaultAsync(es => es.EventId == eventId && es.SeatId == seatId);

            if (eventSeat == null)
            {
                return NotFound(new { message = "This seat is not configured for the specified event." });
            }

            if (eventSeat.Status != SeatStatus.Free)
            {
                return BadRequest(new { message = "Seat is no longer available." });
            }

            eventSeat.Status = SeatStatus.ReservedTemp;
            eventSeat.LockedUntil = DateTime.UtcNow.AddMinutes(10);
            eventSeat.ReservedByUserId = userId;

            try
            {
                await _context.SaveChangesAsync();

                await _hubContext.Clients.All.SendAsync("ReceiveSeatStatusUpdate", eventId, seatId, SeatStatus.ReservedTemp);

                return Ok(new
                {
                    message = "Seat successfully locked for 10 minutes.",
                    lockUntil = eventSeat.LockedUntil
                });
            }
            catch (DbUpdateConcurrencyException)
            {
                return BadRequest(new { message = "Another user just locked this seat. Please choose another one." });
            }
        }
    }
}