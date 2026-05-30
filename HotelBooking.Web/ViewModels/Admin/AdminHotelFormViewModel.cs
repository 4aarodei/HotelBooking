using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace HotelBooking.ViewModels.Admin;

public class AdminHotelFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Hotel name is required")]
    [StringLength(150, ErrorMessage = "Hotel name must be shorter than 150 characters")]
    public string? Name { get; set; }

    [Required(ErrorMessage = "City is required")]
    [StringLength(120, ErrorMessage = "City must be shorter than 120 characters")]
    public string? City { get; set; }

    [Required(ErrorMessage = "Address is required")]
    [StringLength(200, ErrorMessage = "Address must be shorter than 200 characters")]
    public string? Address { get; set; }

    [StringLength(1000, ErrorMessage = "Description must be shorter than 1000 characters")]
    public string? Description { get; set; }

    public List<IFormFile> Photos { get; set; } = [];
    public List<Guid> RemoveImageIds { get; set; } = [];
    public IReadOnlyList<AdminImageViewModel> ExistingImages { get; set; } = [];
    public IReadOnlyList<AdminRoomListItemViewModel> Rooms { get; set; } = [];
}
