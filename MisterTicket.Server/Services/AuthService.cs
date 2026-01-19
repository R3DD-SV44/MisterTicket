using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MisterTicket.Server.Data;
using MisterTicket.Server.Models;
using MisterTicket.Server.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MisterTicket.Server.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _config;

    public AuthService(ApplicationDbContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    public async Task<(bool Success, string Message)> RegisterAsync(UserDto dto)
    {
        if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            return (false, "Cet email est déjà utilisé.");

        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email,
            Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = UserRole.Customer
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return (true, "Utilisateur créé avec succès.");
    }

    public async Task<(bool Success, string Token, string Message)> LoginAsync(string email, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.Password))
        {
            return (false, null, "Email ou mot de passe incorrect.");
        }

        var token = GenerateJwtToken(user);
        return (true, token, "Connexion réussie.");
    }

    private string GenerateJwtToken(User user)
    {
        var claims = new[] {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "MisterTicket",
            audience: "MisterTicket",
            claims: claims,
            expires: DateTime.Now.AddHours(3),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}