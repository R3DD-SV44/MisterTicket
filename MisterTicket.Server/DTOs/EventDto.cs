namespace MisterTicket.Server.DTOs;

public class EventDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int SceneId { get; set; } // Utilise l'ID simple au lieu de l'objet Scene
}