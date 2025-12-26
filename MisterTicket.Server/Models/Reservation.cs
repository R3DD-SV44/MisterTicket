using System.ComponentModel.DataAnnotations;

namespace MisterTicket.Server.Models
{
    public enum ReservationStatus { OnGoing, Paid, Canceled }

    // Models/Reservation.cs

    public class Reservation
    {
        [Key]
        public int Id { get; set; }

        // Changez "string" en "int" pour correspondre à User.Id
        public int UserId { get; set; }

        // Assurez-vous d'avoir bien ajouté l'EventId suite à nos discussions
        public int EventId { get; set; }

        public DateTime ReservationDate { get; set; } = DateTime.UtcNow;
        public ReservationStatus Status { get; set; } = ReservationStatus.OnGoing;
        public List<Seat> SelectedSeats { get; set; } = new();
    }
}