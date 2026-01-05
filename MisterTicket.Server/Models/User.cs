namespace MisterTicket.Server.Models;

public enum UserRole { Admin, Organiser, Customer }


public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty; 
    public string Password{ get; set; } = string.Empty; 
    public UserRole Role { get; set; } = UserRole.Customer; 
    public List<Reservation> Reservations { get; set; } = new();
}