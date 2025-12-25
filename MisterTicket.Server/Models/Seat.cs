namespace MisterTicket.Server.Models;

public enum SeatStatus { Free, ReservedTemp, Paid }

public class Seat
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Row { get; set; } = string.Empty;   
    public SeatStatus Status { get; set; } = SeatStatus.Free;
    public decimal Price { get; set; }  
    public DateTime? LockedUntil { get; set; }
    public string? ReservedByUserId { get; set; }
}