using KbuPortal.Data;
using KbuPortal.Models;
using kbu_portal.ViewModels.Announcements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace kbu_portal.Controllers;

[Authorize]
public class AnnouncementsController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public AnnouncementsController(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string? roleFilter)
    {
        var query = _db.Announcements
            .AsNoTracking()
            .Include(a => a.CreatedBy)
            .AsQueryable();

        if (User.IsInRole("Teacher"))
        {
            query = query.Where(a => a.TargetRole == "All" || a.TargetRole == "Teacher");
        }
        else if (User.IsInRole("Student"))
        {
            query = query.Where(a => a.TargetRole == "All" || a.TargetRole == "Student");
        }

        if (!string.IsNullOrWhiteSpace(roleFilter) && roleFilter != "All")
        {
            if (User.IsInRole("Admin"))
            {
                query = query.Where(a => a.TargetRole == roleFilter);
            }
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(a => a.Title.Contains(term) || a.Content.Contains(term));
        }

        ViewBag.Search = search;
        ViewBag.RoleFilter = GetRoleFilterOptions(roleFilter);

        var announcements = await query
            .OrderByDescending(a => a.IsPinned)
            .ThenByDescending(a => a.CreatedAt)
            .Select(a => new AnnouncementViewModel
            {
                Id = a.Id,
                Title = a.Title,
                Content = a.Content,
                IsPinned = a.IsPinned,
                TargetRole = a.TargetRole,
                CreatedAt = a.CreatedAt,
                CreatedByName = (a.CreatedBy.FirstName ?? string.Empty) + " " + (a.CreatedBy.LastName ?? string.Empty),
                CreatedByEmail = a.CreatedBy.Email ?? string.Empty
            })
            .ToListAsync();

        return View(announcements);
    }

    private List<SelectListItem> GetRoleFilterOptions(string? selected)
    {
        var options = new List<string> { "All" };

        if (User.IsInRole("Admin"))
        {
            options.Add("Student");
            options.Add("Teacher");
        }
        else if (User.IsInRole("Teacher"))
        {
            options.Add("Teacher");
        }
        else if (User.IsInRole("Student"))
        {
            options.Add("Student");
        }

        return options.Select(r => new SelectListItem
        {
            Value = r,
            Text = r,
            Selected = r == (selected ?? "All")
        }).ToList();
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public IActionResult Create()
    {
        return View(new AnnouncementViewModel());
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AnnouncementViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = _userManager.GetUserId(User);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Forbid();
        }

        var announcement = new Announcement
        {
            Title = model.Title,
            Content = model.Content,
            IsPinned = model.IsPinned,
            TargetRole = model.TargetRole,
            CreatedAt = DateTime.UtcNow,
            CreatedById = userId
        };

        _db.Announcements.Add(announcement);
        await _db.SaveChangesAsync();

        TempData["StatusMessage"] = "Announcement created.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var announcement = await _db.Announcements.FindAsync(id);
        if (announcement == null)
        {
            return NotFound();
        }

        var model = new AnnouncementViewModel
        {
            Id = announcement.Id,
            Title = announcement.Title,
            Content = announcement.Content,
            IsPinned = announcement.IsPinned,
            TargetRole = announcement.TargetRole
        };

        return View(model);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AnnouncementViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var announcement = await _db.Announcements.FindAsync(id);
        if (announcement == null)
        {
            return NotFound();
        }

        announcement.Title = model.Title;
        announcement.Content = model.Content;
        announcement.IsPinned = model.IsPinned;
        announcement.TargetRole = model.TargetRole;

        await _db.SaveChangesAsync();

        TempData["StatusMessage"] = "Announcement updated.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var announcement = await _db.Announcements.FindAsync(id);
        if (announcement == null)
        {
            return NotFound();
        }

        _db.Announcements.Remove(announcement);
        await _db.SaveChangesAsync();

        TempData["StatusMessage"] = "Announcement deleted.";
        return RedirectToAction(nameof(Index));
    }
}
