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

        [HttpGet("{id}", Name = "GetPriceZoneById")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetPriceZoneById(int id)
        {
            var pz = await _context.PriceZones.FindAsync(id);
            if (pz != null)
            {
                return Ok(pz);
            }
            return NotFound(new { message = $"The Price Zone with ID {id} does not exist." });
        }

        [HttpPost("create")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PriceZoneDto>> CreatePriceZone(PriceZoneDto dto)
        {
            if (!await _context.Scenes.AnyAsync(s => s.Id == dto.SceneId))
            {
                return NotFound(new { message = $"Scene {dto.SceneId} not found." });
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

        [HttpPut("modify/{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ModifyPriceZone(int id, PriceZoneDto dto)
        {
            if (id != dto.Id) return BadRequest(new { message = "ID mismatch." });

            var priceZone = await _context.PriceZones
                .Include(pz => pz.Scene)
                .FirstOrDefaultAsync(pz => pz.Id == id);

            if (priceZone == null) return NotFound(new { message = "PriceZone not found." });

            priceZone.Name = dto.Name;
            priceZone.Price = dto.Price;
            priceZone.ColorHex = dto.ColorHex;

            if (dto.Seats != null)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var existingSceneSeats = await _context.Seats
                        .Where(s => s.SceneId == priceZone.SceneId)
                        .ToListAsync();

                    foreach (var sDto in dto.Seats)
                    {
                        if (sDto.Row > priceZone.Scene?.MaxRows || sDto.Column > priceZone.Scene?.MaxColumns)
                        {
                            return BadRequest(new { message = $"Seat {sDto.Number} is out of scene bounds." });
                        }

                        var seat = existingSceneSeats.FirstOrDefault(s => s.Row == sDto.Row && s.Column == sDto.Column);

                        if (seat != null)
                        {
                            seat.PriceZoneId = id;
                            seat.Price = priceZone.Price;
                            seat.Number = sDto.Number;
                        }
                        else
                        {
                            var newSeat = new Seat
                            {
                                Number = sDto.Number,
                                Row = sDto.Row,
                                Column = sDto.Column,
                                PriceZoneId = id,
                                SceneId = priceZone.SceneId,
                                Price = priceZone.Price
                            };
                            _context.Seats.Add(newSeat);
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, new { message = "An error occurred while updating the seats layout." });
                }
            }
            else
            {
                await _context.SaveChangesAsync();
            }

            return NoContent();
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

            var sceneExists = await _context.Scenes.AnyAsync(s => s.Id == priceZone.SceneId);
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