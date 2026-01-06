using Microsoft.AspNetCore.Identity;

namespace HotelBooking.Core.EntitiesModels.Identity;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}
