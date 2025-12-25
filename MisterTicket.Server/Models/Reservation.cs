using System.ComponentModel.DataAnnotations;

namespace MisterTicket.Server.Models
{
    public enum ReservationStatus { EnCours, Payee, Annulee }

    public class Reservation
    {
        [Key]
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty; 
        public DateTime ReservationDate { get; set; } = DateTime.UtcNow;
        public ReservationStatus Status { get; set; } = ReservationStatus.EnCours; 

        public List<Seat> SelectedSeats { get; set; } = new();
    }
}