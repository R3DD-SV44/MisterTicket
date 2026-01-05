namespace MisterTicket.Server.DTOs;

public class SeatDto
{
    public int Id { get; set; }
    public string Number { get; set; } = string.Empty; 
    public int Row { get; set; }
    public int Column { get; set; }
    public decimal Price { get; set; }
    public int PriceZoneId { get; set; }
    public int SceneId { get; set; }
}