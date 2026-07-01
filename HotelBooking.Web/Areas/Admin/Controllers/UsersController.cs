using HotelBooking.Application.Security;
using HotelBooking.Infrastructure.Identity;
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
    private readonly SignInManager<ApplicationUser> _signInManager;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _signInManager = signInManager;
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

        var stampResult = await _userManager.UpdateSecurityStampAsync(user);
        if (!stampResult.Succeeded)
        {
            AddIdentityErrors(stampResult);
            return RedirectToAction(nameof(Index));
        }

        var currentUserId = _userManager.GetUserId(User);
        if (string.Equals(currentUserId, user.Id, StringComparison.Ordinal))
        {
            // If the currently authenticated SuperAdmin changed their own role,
            // force re-login so the new permissions are applied immediately.
            await _signInManager.SignOutAsync();
            TempData["InfoMessage"] = "Role was updated. Please sign in again.";
            return Redirect("~/Identity/Account/Login");
        }

        return RedirectToAction(nameof(Index));
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        TempData["ErrorMessage"] = string.Join(" ", result.Errors.Select(e => e.Description));
    }
}
