using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
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

        [HttpPost("lock/{id}")]
        public async Task<IActionResult> LockSeat(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var seat = await _context.Seats.FindAsync(id);
                if (seat == null || seat.Status != SeatStatus.Free)
                    return BadRequest("Siège déjà réservé ou indisponible.");

                seat.Status = SeatStatus.ReservedTemp;
                seat.LockedUntil = DateTime.UtcNow.AddMinutes(10);
                seat.ReservedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await _hubContext.Clients.All.SendAsync("ReceiveSeatStatusUpdate", seat.Id, seat.Status);
                return Ok(new { message = "Siège bloqué", lockUntil = seat.LockedUntil });
            }
            catch
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "Erreur lors du verrouillage");
            }
        }
    }
}