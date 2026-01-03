namespace MisterTicket.Server.Models;

public class PriceZone
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string ColorHex { get; set; } = "#FFFFFF";
    public int SceneId { get; set; }
    public Scene Scene { get; set; }
}