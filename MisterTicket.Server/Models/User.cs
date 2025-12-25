namespace MisterTicket.Server.Models;

public enum UserRole { Admin, Organisateur, Client }


public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty; 
    public string PasswordHash { get; set; } = string.Empty; 
    public UserRole Role { get; set; } = UserRole.Client; 

    public List<Reservation> Reservations { get; set; } = new();
}