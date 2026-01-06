using Microsoft.AspNetCore.Identity;

namespace HotelBooking.Domain.Entities.Identity;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}
