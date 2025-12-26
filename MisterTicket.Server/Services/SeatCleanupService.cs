using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using MisterTicket.Server.Data;
using MisterTicket.Server.Hubs;
using MisterTicket.Server.Models;

namespace MisterTicket.Server.Services
{
    public class SeatCleanupService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly IHubContext<TicketHub> _hubContext;

        public SeatCleanupService(IServiceProvider services, IHubContext<TicketHub> hubContext)
        {
            _services = services;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _services.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    var expiredSeats = context.Seats
                        .Where(s => s.Status == SeatStatus.ReservedTemp && s.LockedUntil < DateTime.UtcNow)
                        .ToList();

                    foreach (var seat in expiredSeats)
                    {
                        seat.Status = SeatStatus.Free;
                        seat.LockedUntil = null;

                        var relatedRes = await context.Reservations
                        .Where(r => r.SelectedSeats.Any(s => s.Id == seat.Id) && r.Status == ReservationStatus.OnGoing)
                        .FirstOrDefaultAsync();

                        if (relatedRes != null) relatedRes.Status = ReservationStatus.Canceled;

                        await _hubContext.Clients.All.SendAsync("ReceiveSeatStatusUpdate", seat.Id, SeatStatus.Free);
                    }

                    if (expiredSeats.Any()) await context.SaveChangesAsync();
                }
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
}
