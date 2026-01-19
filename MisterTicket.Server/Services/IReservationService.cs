using MisterTicket.Server.DTOs;
using MisterTicket.Server.Models;

namespace MisterTicket.Server.Services;

public interface IReservationService
{
    Task<(bool Success, Reservation Reservation, string Error, int HttpStatusCode)> CreateReservationAsync(int userId, ReservationDto request);
}