namespace MisterTicket.Server.Models;

public enum SeatStatus { Free, ReservedTemp, Paid }
public class EventSeat
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public Event? Event { get; set; }
    public int SeatId { get; set; }
    public Seat? Seat { get; set; }
    public SeatStatus Status { get; set; } = SeatStatus.Free;
    public DateTime? LockedUntil { get; set; }
    public int? ReservedByUserId { get; set; }
}