using System.ComponentModel.DataAnnotations;

namespace HotelBooking.ViewModels.Admin;

public class AdminHotelFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Назва обов'язкова")]
    [StringLength(150, ErrorMessage = "Назва має бути коротшою за 150 символів")]
    public string? Name { get; set; }

    [Required(ErrorMessage = "Місто обов'язкове")]
    [StringLength(120, ErrorMessage = "Місто має бути коротшим за 120 символів")]
    public string? City { get; set; }

    [Required(ErrorMessage = "Адреса обов'язкова")]
    [StringLength(200, ErrorMessage = "Адреса має бути коротшою за 200 символів")]
    public string? Address { get; set; }

    [StringLength(1000, ErrorMessage = "Опис має бути коротшим за 1000 символів")]
    public string? Description { get; set; }
}
