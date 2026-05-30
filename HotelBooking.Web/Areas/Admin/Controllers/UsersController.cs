using HotelBooking.Domain.Entities.Identity;
using HotelBooking.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Web.Areas.Admin.Controllers;

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
        var roles = _roleManager.Roles
            .Select(r => r.Name)
            .Where(r => r != null)
            .Cast<string>()
            .ToList();

        var model = new List<UserRolesVm>();

        foreach (var user in users)
        {
            model.Add(new UserRolesVm
            {
                UserId = user.Id,
                Email = user.Email!,
                Roles = (await _userManager.GetRolesAsync(user)).ToList(),
                AllRoles = roles
            });
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateRole(string userId, string role)
    {
        if (string.IsNullOrWhiteSpace(role) || !await _roleManager.RoleExistsAsync(role))
        {
            return BadRequest("Selected role does not exist.");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Contains(AppRoles.SuperAdmin) && role != AppRoles.SuperAdmin)
        {
            var superAdmins = await _userManager.GetUsersInRoleAsync(AppRoles.SuperAdmin);
            if (superAdmins.Count <= 1)
            {
                return BadRequest("Cannot remove the last SuperAdmin.");
            }
        }

        var rolesToRemove = currentRoles.Where(r => r != role).ToList();
        if (rolesToRemove.Count > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removeResult.Succeeded)
            {
                AddIdentityErrors(removeResult);
                return RedirectToAction(nameof(Index));
            }
        }

        if (!currentRoles.Contains(role))
        {
            var addResult = await _userManager.AddToRoleAsync(user, role);
            if (!addResult.Succeeded)
            {
                AddIdentityErrors(addResult);
                return RedirectToAction(nameof(Index));
            }
        }

        return RedirectToAction(nameof(Index));
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        TempData["ErrorMessage"] = string.Join(" ", result.Errors.Select(e => e.Description));
    }
}
