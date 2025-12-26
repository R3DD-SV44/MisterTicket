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

        [HttpPost("lock/{eventId}/{seatId}")]
        public async Task<IActionResult> LockSeat(int eventId, int seatId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var eventSeat = await _context.EventSeats
                .FirstOrDefaultAsync(es => es.EventId == eventId && es.SeatId == seatId);

            if (eventSeat == null) return NotFound("Siège non configuré pour cet événement.");
            if (eventSeat.Status != SeatStatus.Free) return BadRequest("Siège indisponible.");

            eventSeat.Status = SeatStatus.ReservedTemp;
            eventSeat.LockedUntil = DateTime.UtcNow.AddMinutes(10);
            eventSeat.ReservedByUserId = userId;

            await _context.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("ReceiveSeatStatusUpdate", eventId, seatId, SeatStatus.ReservedTemp);

            return Ok(new { message = "Siège bloqué", lockUntil = eventSeat.LockedUntil });
        }
    }
}