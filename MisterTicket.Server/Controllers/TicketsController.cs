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
        // 1. Authentification
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new { message = "User is not authenticated." });
        }

        // 2. Récupération de la réservation avec les sièges ET l'événement
        var reservation = await _context.Reservations
            .Include(r => r.SelectedSeats)
            .Include(r => r.Event)
            .FirstOrDefaultAsync(r => r.Id == reservationId);

        // 3. Vérifications de sécurité et d'état
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

        // 4. Données du billet
        var eventName = reservation.Event?.Name ?? "Événement MisterTicket";
        var eventDate = reservation.Event?.Date ?? reservation.ReservationDate;

        // 5. Génération du QR Code
        using var qrGenerator = new QRCodeGenerator();
        var qrData = qrGenerator.CreateQrCode($"TICKET-{reservation.Id}-USER-{reservation.UserId}", QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrData);
        byte[] qrCodeImage = qrCode.GetGraphic(20);

        // 6. Construction du PDF avec QuestPDF
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A6);
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Helvetica));

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

        // 7. Envoi du fichier
        var pdfBytes = document.GeneratePdf();
        return File(pdfBytes, "application/pdf", $"Ticket_{reservation.Event?.Name}_{reservationId}.pdf");
    }

    [HttpGet("{reservationId}/qrcode")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTicketQrCode(int reservationId)
    {
        // 1. Vérification de l'utilisateur
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out int userId)) return Unauthorized();

        // 2. Récupération de la réservation
        var reservation = await _context.Reservations
            .FirstOrDefaultAsync(r => r.Id == reservationId);

        if (reservation == null)
            return NotFound(new { message = "Reservation not found." });

        // 3. Sécurité : Vérifier que c'est bien le propriétaire
        if (reservation.UserId != userId)
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Access denied." });

        // 4. Vérifier que le paiement est fait
        if (reservation.Status != ReservationStatus.Paid)
            return BadRequest(new { message = "QR Code is only available for paid tickets." });

        // 5. Génération du QR Code
        using var qrGenerator = new QRCodeGenerator();
        // Le contenu du QR Code est une chaîne unique pour validation à l'entrée
        var qrData = qrGenerator.CreateQrCode($"TICKET-ID:{reservation.Id}|USER:{reservation.UserId}", QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrData);
        byte[] qrCodeImage = qrCode.GetGraphic(20);

        // 6. Retourne l'image brute au format PNG
        return File(qrCodeImage, "image/png");
    }
}