namespace MisterTicket.Server.DTOs;

public class ReservationDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int EventId { get; set; }
    public DateTime ReservationDate { get; set; }
    public string Status { get; set; } = "OnGoing";
    public List<int> SeatIds { get; set; } = new();
}