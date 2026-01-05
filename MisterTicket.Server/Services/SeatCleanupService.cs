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
            await Task.Delay(10000, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _services.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                        var expiredEventSeats = await context.EventSeats
                            .Where(es => es.Status == SeatStatus.ReservedTemp && es.LockedUntil < DateTime.UtcNow)
                            .ToListAsync(stoppingToken);

                        foreach (var eventSeat in expiredEventSeats)
                        {
                            var userId = eventSeat.ReservedByUserId;

                            if (userId.HasValue)
                            {
                                var relatedRes = await context.Reservations
                                    .Include(r => r.SelectedSeats)
                                    .Where(r => r.UserId == userId.Value
                                           && r.Status == ReservationStatus.OnGoing
                                           && r.SelectedSeats.Any(s => s.Id == eventSeat.SeatId))
                                    .FirstOrDefaultAsync();

                                if (relatedRes != null)
                                {
                                    relatedRes.Status = ReservationStatus.Canceled;
                                }
                            }

                            eventSeat.Status = SeatStatus.Free;
                            eventSeat.LockedUntil = null;
                            eventSeat.ReservedByUserId = null;

                            await _hubContext.Clients.All.SendAsync("ReceiveSeatStatusUpdate", eventSeat.EventId, eventSeat.SeatId, SeatStatus.Free);
                        }

                        if (expiredEventSeats.Any())
                        {
                            await context.SaveChangesAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Cleanup error: {ex.Message}");
                }
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
}
