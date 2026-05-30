using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace HotelBooking.ViewModels.Admin;

public class AdminRoomFormViewModel
{
    public Guid? Id { get; set; }
    public Guid HotelId { get; set; }
    public string HotelName { get; set; } = string.Empty;
    public string DraftUploadId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Room name is required")]
    [StringLength(150, ErrorMessage = "Room name must be shorter than 150 characters")]
    public string? Name { get; set; }

    [StringLength(1200, ErrorMessage = "Description must be shorter than 1200 characters")]
    public string? Description { get; set; }

    [StringLength(1000, ErrorMessage = "Amenities must be shorter than 1000 characters")]
    public string? Amenities { get; set; }

    [Range(1, 20, ErrorMessage = "Capacity must be between 1 and 20")]
    public int Capacity { get; set; } = 2;

    [Range(0.01, 999999, ErrorMessage = "Price must be greater than zero")]
    public decimal PricePerNight { get; set; }

    [Range(1, 100, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; } = 1;

    public bool IncludesBreakfast { get; set; }
    public bool HasPrivateBathroom { get; set; } = true;
    public bool HasSaunaAccess { get; set; }
    public bool HasBalcony { get; set; }
    public bool HasWorkspace { get; set; }
    public bool HasAirConditioning { get; set; }

    public bool IsActive { get; set; } = true;
    public List<IFormFile> Photos { get; set; } = [];
    public List<Guid> RemoveImageIds { get; set; } = [];
    public IReadOnlyList<AdminImageViewModel> ExistingImages { get; set; } = [];
}
