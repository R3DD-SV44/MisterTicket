using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MisterTicket.Server.Data;
using MisterTicket.Server.DTOs;
using MisterTicket.Server.Models;
using MisterTicket.Server.Services;
using System.Security.Claims;

namespace MisterTicket.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ApplicationDbContext _context;

    public AuthController(IAuthService authService, ApplicationDbContext context)
    {
        _authService = authService;
        _context = context;
    }

    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(UserDto dto)
    {
        var result = await _authService.RegisterAsync(dto);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return Created(string.Empty, new { message = result.Message });
    }

    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest login)
    {
        var result = await _authService.LoginAsync(login.Email, login.Password);

        if (!result.Success)
        {
            return Unauthorized(new { message = result.Message });
        }

        return Ok(new { token = result.Token });
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAllUsers()
    {
        var users = await _context.Users
            .Select(u => new UserDto
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                Role = u.Role.ToString()
            })
            .ToListAsync();

        return Ok(users);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("users/{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUserRole(int id, [FromBody] UserDto dto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound(new { message = "Utilisateur introuvable" });

        if (!string.IsNullOrEmpty(dto.Role) && Enum.TryParse<UserRole>(dto.Role, true, out var newRole))
        {
            user.Role = newRole;
        }

        if (!string.IsNullOrEmpty(dto.Name)) user.Name = dto.Name;
        if (!string.IsNullOrEmpty(dto.Email)) user.Email = dto.Email;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Utilisateur mis à jour" });
    }

    [Authorize]
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var currentUserIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var currentUserRole = User.FindFirstValue(ClaimTypes.Role);

        if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out int currentUserId))
        {
            return Unauthorized(new { message = "Utilisateur non authentifié." });
        }

        var userToDelete = await _context.Users.FindAsync(id);
        if (userToDelete == null)
        {
            return NotFound(new { message = $"L'utilisateur avec l'ID {id} n'existe pas." });
        }

        bool isAdmin = currentUserRole == UserRole.Admin.ToString();
        bool isSelf = currentUserId == id;

        if (!isAdmin && !isSelf)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Vous n'avez pas l'autorisation de supprimer ce compte." });
        }

        _context.Users.Remove(userToDelete);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return BadRequest(new { message = "Erreur lors de la suppression de l'utilisateur (des données y sont peut-être liées)." });
        }

        return NoContent();
    }
}