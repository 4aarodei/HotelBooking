namespace HotelBooking.Models.ViewModels
{
    public class CreateBookingRequest
    {
        public Guid RoomId { get; set; }
        public DateTime CheckIn { get; set; }
        public DateTime CheckOut { get; set; }
    }

}
