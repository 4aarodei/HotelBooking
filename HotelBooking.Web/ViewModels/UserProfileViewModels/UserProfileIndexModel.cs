using System.ComponentModel.DataAnnotations;
using HotelBooking.Infrastructure.Identity;

namespace HotelBooking.ViewModels.UserProfileViewModels
{
    public class UserProfileIndexModel
    {
        public string? Email { get; set; }

        [Required(ErrorMessage = "Ім'я є обов'язковим")]
        [Display(Name = "Ім'я")]
        public string? FirstName { get; set; }

        [Required(ErrorMessage = "Прізвище є обов'язковим")]
        [Display(Name = "Прізвище")]
        public string? LastName { get; set; }
    }
}
