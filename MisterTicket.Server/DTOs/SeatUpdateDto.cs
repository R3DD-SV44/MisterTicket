namespace MisterTicket.Server.DTOs
{
    public class SeatUpdateDto
    {
        public int Row { get; set; }
        public int Column { get; set; }
        public int? PriceZoneId { get; set; }
    }
}
