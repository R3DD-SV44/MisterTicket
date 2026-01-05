namespace MisterTicket.Server.Models;

public enum PaymentStatus { Pending, Success, Failed }

public class Payment
{
    public int Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public int ReservationId { get; set; }
    public Reservation? Reservation { get; set; }
}