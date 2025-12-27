namespace MisterTicket.Server.Models;


public class Seat
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty; 
    public decimal Price { get; set; }  
    public int? ReservedByUserId { get; set; }
    public int PriceZoneId { get; set; }
    public int Row { get; set; }
    public int Column { get; set; }
    public PriceZone? PriceZone { get; set; }
    public int SceneId { get; set; }
}