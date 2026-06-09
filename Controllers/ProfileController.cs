using KbuPortal.Models;
using kbu_portal.ViewModels.Profile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace kbu_portal.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private const long MaxProfilePhotoBytes = 2 * 1024 * 1024;
    private static readonly HashSet<string> AllowedProfilePhotoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".gif",
        ".webp"
    };

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
            await PopulateProfileMetadataAsync(user, model);
            return View("Index", model);
        }

        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.StudentId = model.StudentId;
        user.Faculty = model.Faculty;
        user.Major = model.Major;

        if (model.PhotoFile != null && model.PhotoFile.Length > 0)
        {
            var extension = Path.GetExtension(model.PhotoFile.FileName);
            if (!AllowedProfilePhotoExtensions.Contains(extension))
            {
                ModelState.AddModelError(nameof(model.PhotoFile), "Upload a JPG, PNG, GIF, or WebP image.");
                await PopulateProfileMetadataAsync(user, model);
                return View("Index", model);
            }

            if (model.PhotoFile.Length > MaxProfilePhotoBytes)
            {
                ModelState.AddModelError(nameof(model.PhotoFile), "Profile photo must be 2 MB or smaller.");
                await PopulateProfileMetadataAsync(user, model);
                return View("Index", model);
            }

            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsFolder);

            var fileName = $"{user.Id}_{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
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

        await PopulateProfileMetadataAsync(user, model);

        return View("Index", model);
    }

    private async Task PopulateProfileMetadataAsync(ApplicationUser user, ProfileViewModel model)
    {
        var roles = await _userManager.GetRolesAsync(user);
        model.Role = roles.FirstOrDefault() ?? "Student";
        model.Email = user.Email ?? string.Empty;
        model.ProfilePhoto = user.ProfilePhoto;
    }
}
