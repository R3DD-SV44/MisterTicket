using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MisterTicket.Server.Data;
using MisterTicket.Server.Models;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Security.Claims;
using System.IO.Compression;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TicketsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public TicketsController(ApplicationDbContext context)
    {
        _context = context;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [HttpGet("{reservationId}/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTicketPdf(int reservationId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new { message = "User is not authenticated." });
        }

        var reservation = await _context.Reservations
            .Include(r => r.SelectedSeats)
            .Include(r => r.Event)
            .FirstOrDefaultAsync(r => r.Id == reservationId);

        if (reservation == null)
        {
            return NotFound(new { message = $"Reservation with ID {reservationId} not found." });
        }

        if (reservation.UserId != userId)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You are not authorized to download this ticket." });
        }

        if (reservation.Status != ReservationStatus.Paid)
        {
            return BadRequest(new { message = "Ticket is only available after successful payment." });
        }

        var eventName = reservation.Event?.Name ?? "Événement MisterTicket";
        var eventDate = reservation.Event?.Date ?? reservation.ReservationDate;

        using var qrGenerator = new QRCodeGenerator();
        var qrData = qrGenerator.CreateQrCode($"TICKET-{reservation.Id}-USER-{reservation.UserId}", QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrData);
        byte[] qrCodeImage = qrCode.GetGraphic(20);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A6);
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.SegoeUI));

                page.Header().Text("VOTRE BILLET ELECTRONIQUE")
                    .SemiBold().FontSize(14).FontColor(Colors.Blue.Medium).AlignCenter();

                page.Content().Column(col =>
                {
                    col.Spacing(5);
                    col.Item().PaddingTop(10).Text(eventName).FontSize(12).ExtraBold();
                    col.Item().Text($"Date : {eventDate:dd/MM/yyyy HH:mm}");
                    col.Item().Text($"Client : {User.Identity?.Name ?? "Client MisterTicket"}");

                    col.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    col.Item().Text("DÉTAILS DES SIÈGES").SemiBold().FontSize(9);
                    foreach (var seat in reservation.SelectedSeats)
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text($"- Rang {seat.Row}, Siège {seat.Number}");
                            row.ConstantItem(30).AlignRight().Text($"{seat.Price}€");
                        });
                    }

                    col.Item().PaddingTop(10).AlignCenter().Image(qrCodeImage).FitWidth();
                    col.Item().AlignCenter().Text($"Réservation n°{reservation.Id}").FontSize(8).FontColor(Colors.Grey.Medium);
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Billet généré par ");
                    x.Span("MisterTicket").SemiBold().FontColor(Colors.Blue.Medium);
                });
            });
        });

        var pdfBytes = document.GeneratePdf();
        return File(pdfBytes, "application/pdf", $"Ticket_{reservation.Event?.Name}_{reservationId}.pdf");
    }

    [HttpGet("{reservationId}/qrcode")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTicketQrCode(int reservationId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out int userId)) return Unauthorized();

        var reservation = await _context.Reservations
            .FirstOrDefaultAsync(r => r.Id == reservationId);

        if (reservation == null)
            return NotFound(new { message = "Reservation not found." });

        if (reservation.UserId != userId)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Access denied." });

        if (reservation.Status != ReservationStatus.Paid)
            return BadRequest(new { message = "QR Code is only available for paid tickets." });

        using var qrGenerator = new QRCodeGenerator();
        var qrData = qrGenerator.CreateQrCode($"TICKET-ID:{reservation.Id}|USER:{reservation.UserId}", QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrData);
        byte[] qrCodeImage = qrCode.GetGraphic(20);

        return File(qrCodeImage, "image/png");
    }

    [HttpGet("{reservationId}/zip")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTicketZip(int reservationId)
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out int userId)) return Unauthorized();

        var reservation = await _context.Reservations
            .Include(r => r.SelectedSeats)
            .Include(r => r.Event)
            .FirstOrDefaultAsync(r => r.Id == reservationId);

        if (reservation == null) return NotFound("Reservation not found.");
        if (reservation.UserId != userId) return Forbid();
        if (reservation.Status != ReservationStatus.Paid) return BadRequest("Ticket must be paid.");

        using var qrGenerator = new QRCodeGenerator();
        var qrData = qrGenerator.CreateQrCode($"TICKET-{reservation.Id}-USER-{reservation.UserId}", QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrData);
        byte[] qrCodeBytes = qrCode.GetGraphic(20);

        var eventName = reservation.Event?.Name ?? "Événement";
        var eventDate = reservation.Event?.Date ?? reservation.ReservationDate;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A6);
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.SegoeUI));

                page.Header().Text("VOTRE BILLET ELECTRONIQUE").SemiBold().FontSize(14).FontColor(Colors.Blue.Medium).AlignCenter();
                page.Content().Column(col =>
                {
                    col.Spacing(5);
                    col.Item().PaddingTop(10).Text(eventName).FontSize(12).ExtraBold();
                    col.Item().Text($"Date : {eventDate:dd/MM/yyyy HH:mm}");
                    col.Item().PaddingTop(10).AlignCenter().Image(qrCodeBytes).FitWidth();
                    col.Item().AlignCenter().Text($"Réservation n°{reservation.Id}").FontSize(8);
                });
            });
        });
        byte[] pdfBytes = document.GeneratePdf();

        using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            var pdfEntry = archive.CreateEntry($"Ticket_{reservationId}.pdf");
            using (var entryStream = pdfEntry.Open())
            {
                await entryStream.WriteAsync(pdfBytes, 0, pdfBytes.Length);
            }

            var qrEntry = archive.CreateEntry($"QRCode_{reservationId}.png");
            using (var entryStream = qrEntry.Open())
            {
                await entryStream.WriteAsync(qrCodeBytes, 0, qrCodeBytes.Length);
            }
        }

        memoryStream.Position = 0;
        return File(memoryStream.ToArray(), "application/zip", $"Pack_Tickets_{reservationId}.zip");
    }
}