using System.ComponentModel.DataAnnotations;

namespace HotelBooking.Web.ViewModels
{
    public class CreateBookingRequest : IValidatableObject
    {
        [Required(ErrorMessage = "Оберіть номер.")]
        public Guid RoomId { get; set; }

        [Required(ErrorMessage = "Вкажіть дату заїзду.")]
        [DataType(DataType.Date)]
        public DateOnly CheckIn { get; set; }

        [Required(ErrorMessage = "Вкажіть дату виїзду.")]
        [DataType(DataType.Date)]
        public DateOnly CheckOut { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (CheckOut <= CheckIn)
            {
                yield return new ValidationResult(
                    "Дата виїзду має бути пізніше дати заїзду.",
                    new[] { nameof(CheckOut) });
            }
        }
    }
}
