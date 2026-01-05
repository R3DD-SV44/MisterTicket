using System.ComponentModel.DataAnnotations;

namespace MisterTicket.Server.Models
{
    public enum ReservationStatus { OnGoing = 1, Paid = 2, Canceled =3 }

    public class Reservation
    {
        [Key]
        public int Id { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }
        public int EventId { get; set; }
        public Event? Event { get; set; }
        public DateTime ReservationDate { get; set; } = DateTime.UtcNow;
        public ReservationStatus Status { get; set; } = ReservationStatus.OnGoing;
        public List<Seat>? SelectedSeats { get; set; } = new();
    }
}