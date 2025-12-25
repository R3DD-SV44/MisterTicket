using Microsoft.AspNetCore.SignalR;
using MisterTicket.Server.Models;

namespace MisterTicket.Server.Hubs;

public class TicketHub : Hub
{
    public async Task NotifySeatStatusChanged(int seatId, SeatStatus newStatus)
    {
        await Clients.All.SendAsync("ReceiveSeatStatusUpdate", seatId, newStatus);
    }

    public async Task SendNotification(string message)
    {
        await Clients.Caller.SendAsync("ReceiveNotification", message);
    }
}