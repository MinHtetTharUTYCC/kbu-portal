using KbuPortal.Models;
using kbu_portal.ViewModels.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace kbu_portal.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public ProfileController(
        UserManager<ApplicationUser> userManager,
        IWebHostEnvironment webHostEnvironment)
    {
        _userManager = userManager;
        _webHostEnvironment = webHostEnvironment;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);
        var model = new ProfileViewModel
        {
            Id = user.Id,
            FirstName = user.FirstName ?? string.Empty,
            LastName = user.LastName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            StudentId = user.StudentId,
            Faculty = user.Faculty,
            Major = user.Major,
            ProfilePhoto = user.ProfilePhoto,
            Role = roles.FirstOrDefault() ?? "Student"
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(ProfileViewModel model)
    {
        var userId = _userManager.GetUserId(User);
        var user = await _userManager.FindByIdAsync(userId);

        if (user == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            var roles = await _userManager.GetRolesAsync(user);
            model.Role = roles.FirstOrDefault() ?? "Student";
            model.ProfilePhoto = user.ProfilePhoto;
            return View("Index", model);
        }

        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.StudentId = model.StudentId;
        user.Faculty = model.Faculty;
        user.Major = model.Major;

        // Handle photo upload
        if (model.PhotoFile != null && model.PhotoFile.Length > 0)
        {
            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{user.Id}_{Guid.NewGuid()}_{Path.GetFileName(model.PhotoFile.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await model.PhotoFile.CopyToAsync(stream);
            }

            user.ProfilePhoto = $"/uploads/{fileName}";
        }

        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
        {
            TempData["StatusMessage"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        var roles2 = await _userManager.GetRolesAsync(user);
        model.Role = roles2.FirstOrDefault() ?? "Student";
        model.ProfilePhoto = user.ProfilePhoto;

        return View("Index", model);
    }
}
