namespace MisterTicket.Server.DTOs;

public class SceneDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MaxRows { get; set; }
    public int MaxColumns { get; set; }
    public string? ImageUrl { get; set; } 
    public IFormFile? ImageFile { get; set; } 
}