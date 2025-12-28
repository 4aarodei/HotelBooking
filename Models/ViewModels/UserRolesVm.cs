namespace HotelBooking.Models.ViewModels;

    public class UserRolesVm
    {
        public string UserId { get; set; } = null!;
        public string Email { get; set; } = null!;
        public IList<string> Roles { get; set; } = new List<string>();
        public List<string> AllRoles { get; set; } = new();
    }


