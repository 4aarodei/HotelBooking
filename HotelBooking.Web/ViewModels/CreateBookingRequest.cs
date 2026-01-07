using System.ComponentModel.DataAnnotations;

namespace HotelBooking.Web.ViewModels
{
    public class CreateBookingRequest : IValidatableObject
    {
        [Required] public Guid RoomId { get; set; }

        [Required][DataType(DataType.Date)] public DateTime CheckIn { get; set; }

        [Required][DataType(DataType.Date)] public DateTime CheckOut { get; set; }


        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (CheckOut <= CheckIn)
            {
                yield return new ValidationResult(
                    "Check-out має бути пізніше Check-in",
                    new[] { nameof(CheckOut) });
            }
        }

    }
}