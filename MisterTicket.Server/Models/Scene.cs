namespace MisterTicket.Server.Models;

public class Scene
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; 
    public int Capacity { get; set; }

    public List<Seat> Seats { get; set; } = new();
    public List<PriceZone> PriceZones { get; set; } = new();
}

public class PriceZone
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; 
    public decimal Price { get; set; }
    public string ColorHex { get; set; } = "#FFFFFF"; 
    public int SceneId { get; set; }
}