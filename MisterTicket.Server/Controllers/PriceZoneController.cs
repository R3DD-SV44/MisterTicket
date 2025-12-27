using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MisterTicket.Server.Data;
using MisterTicket.Server.DTOs;
using MisterTicket.Server.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MisterTicket.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PriceZoneController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public PriceZoneController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("create")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IEnumerable<EventDto>>> CreatePriceZone(PriceZoneDto dto)
        {
            var sceneExists = await _context.Scene.AnyAsync(s => s.Id == dto.SceneId);

            if (!sceneExists)
            {
                return NotFound(new { message = $"Cannot create PriceZone: Scene with ID {dto.SceneId} does not exist." });
            }

            var priceZone = new PriceZone
            {
                Name = dto.Name,
                Price = dto.Price,
                ColorHex = dto.ColorHex,
                SceneId = dto.SceneId
            };

            _context.PriceZones.Add(priceZone);
            await _context.SaveChangesAsync();

            dto.Id = priceZone.Id;

            return Ok(dto);
        }

        [HttpDelete("delete_{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeletePriceZone(int id)
        {
            var priceZone = await _context.PriceZones.FindAsync(id);

            if (priceZone == null)
            {
                return NotFound(new { message = $"PriceZone with ID {id} not found." });
            }

            var sceneExists = await _context.Scene.AnyAsync(s => s.Id == priceZone.SceneId);
            if (!sceneExists)
            {
                return NotFound(new { message = $"The scene associated with this PriceZone (ID: {priceZone.SceneId}) does not exist." });
            }

            var isPriceZoneInUse = await _context.Seats.AnyAsync(s => s.PriceZoneId == id);
            if (isPriceZoneInUse)
            {
                return BadRequest(new { message = "Cannot delete PriceZone: it is currently assigned to one or more seats." });
            }

            _context.PriceZones.Remove(priceZone);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return BadRequest(new { message = "An error occurred while deleting the PriceZone from the database." });
            }

            return NoContent();
        }
    }
}
