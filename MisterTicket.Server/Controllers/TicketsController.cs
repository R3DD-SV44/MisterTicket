using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MisterTicket.Server.Data;
using MisterTicket.Server.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QRCoder;

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
    public async Task<IActionResult> GetTicketPdf(int reservationId)
    {
        var reservation = await _context.Reservations
            .Include(r => r.SelectedSeats)
            .FirstOrDefaultAsync(r => r.Id == reservationId);

        if (reservation == null) return NotFound();
        if (reservation.Status != ReservationStatus.Paid)
            return BadRequest("Le billet n'est disponible qu'après paiement.");

        var firstSeat = reservation.SelectedSeats.FirstOrDefault();
        var eventName = "Événement MisterTicket";

        using var qrGenerator = new QRCodeGenerator();
        var qrData = qrGenerator.CreateQrCode($"TICKET-{reservation.Id}-{reservation.UserId}", QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrData);
        byte[] qrCodeImage = qrCode.GetGraphic(20);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A6);
                page.Margin(1, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Text("VOTRE BILLET ELECTRONIQUE").SemiBold().FontSize(14).FontColor(Colors.Blue.Medium);

                page.Content().Column(col =>
                {
                    col.Spacing(5);
                    col.Item().Text($"Événement : {eventName}").FontSize(12).SemiBold();
                    col.Item().Text($"Date : {reservation.ReservationDate:dd/MM/yyyy}");
                    col.Item().Text($"Réservé par : {User.Identity?.Name}");

                    col.Item().PaddingTop(10).LineHorizontal(1);

                    col.Item().Text("Sièges choisis :").SemiBold();
                    foreach (var seat in reservation.SelectedSeats)
                    {
                        col.Item().Text($"- Rangée {seat.Row}, Siège {seat.Number} ({seat.Price}€)");
                    }

                    col.Item().PaddingTop(10).AlignCenter().Image(qrCodeImage).FitWidth();
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Billet généré par ");
                    x.Span("MisterTicket").SemiBold();
                });
            });
        });

        var pdfBytes = document.GeneratePdf();
        return File(pdfBytes, "application/pdf", $"Ticket_{reservationId}.pdf");
    }
}