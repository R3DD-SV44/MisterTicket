namespace MisterTicket.Server.Models;

public enum SeatStatus { Free, ReservedTemp, Paid }

public class Seat
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Row { get; set; } = string.Empty;   
    public decimal Price { get; set; }  
    public int? ReservedByUserId { get; set; }
    public int PriceZoneId { get; set; }
    public PriceZone? PriceZone { get; set; }
}