namespace MisterTicket.Server.Models;

public class Event
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty; 
    public DateTime Date { get; set; }
    public int SceneId { get; set; }
    public Scene? Scene { get; set; }
    public string? ImageUrl { get; set; }
}