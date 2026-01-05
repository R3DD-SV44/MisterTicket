namespace MisterTicket.Server.Models;

public class Scene
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MaxRows { get; set; }
    public int MaxColumns { get; set; }
    public string? ImageUrl { get; set; }
    public List<Seat> Seats { get; set; } = new();
    public List<PriceZone> PriceZones { get; set; } = new();
}
