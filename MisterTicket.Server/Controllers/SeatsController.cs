using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using MisterTicket.Server.Data;
using MisterTicket.Server.Hubs;
using MisterTicket.Server.Models;

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

        [HttpPost("lock/{id}")]
        public async Task<IActionResult> LockSeat(int id)
        {
            var seat = await _context.Seats.FindAsync(id);
            if (seat == null || seat.Status != SeatStatus.Free)
                return BadRequest("Siège non disponible.");

            seat.Status = SeatStatus.ReservedTemp;
            seat.LockedUntil = DateTime.UtcNow.AddMinutes(10);

            await _context.SaveChangesAsync();

            await _hubContext.Clients.All.SendAsync("ReceiveSeatStatusUpdate", seat.Id, seat.Status);

            return Ok(new { message = "Siège bloqué temporairement", lockUntil = seat.LockedUntil });
        }
    }
}
