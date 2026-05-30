using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace HotelBooking.ViewModels.Admin;

public class AdminRoomFormViewModel
{
    public Guid? Id { get; set; }
    public Guid HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Room name is required")]
    [StringLength(150, ErrorMessage = "Room name must be shorter than 150 characters")]
    public string? Name { get; set; }

    [Range(1, 20, ErrorMessage = "Capacity must be between 1 and 20")]
    public int Capacity { get; set; } = 2;

    [Range(0.01, 999999, ErrorMessage = "Price must be greater than zero")]
    public decimal PricePerNight { get; set; }

    [Range(1, 100, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; } = 1;

    public bool IsActive { get; set; } = true;
    public IFormFileCollection? Photos { get; set; }
    public List<Guid> RemoveImageIds { get; set; } = [];
    public IReadOnlyList<AdminImageViewModel> ExistingImages { get; set; } = [];
}
