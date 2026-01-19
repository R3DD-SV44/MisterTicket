using MisterTicket.Server.DTOs;

namespace MisterTicket.Server.Services;

public interface IAuthService
{
    Task<(bool Success, string Message)> RegisterAsync(UserDto dto);
    Task<(bool Success, string Token, string Message)> LoginAsync(string email, string password);
}