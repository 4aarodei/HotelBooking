using HotelBooking.Domain.Entities.Identity;
using HotelBooking.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Areas.Admin.Controllers;

[Authorize(Roles = "SuperAdmin")]
public class UsersController : AdminControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UsersController(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var users = _userManager.Users.ToList();
        var roles = _roleManager.Roles.Select(r => r.Name).ToList();

        var model = new List<UserRolesVm>();

        foreach (var user in users)
        {
            model.Add(new UserRolesVm
            {
                UserId = user.Id,
                Email = user.Email!,
                Roles = await _userManager.GetRolesAsync(user),
                AllRoles = roles
            });
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRole(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        var currentRoles = await _userManager.GetRolesAsync(user);

        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, role);

        return RedirectToAction(nameof(Index));
    }
}
