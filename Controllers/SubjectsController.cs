using KbuPortal.Data;
using KbuPortal.Models;
using kbu_portal.ViewModels.Subjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace kbu_portal.Controllers;

[Authorize(Roles = "Admin")]
public class SubjectsController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public SubjectsController(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var subjects = await _db.Subjects
            .AsNoTracking()
            .Include(s => s.Teacher)
            .Select(s => new SubjectViewModel
            {
                Id = s.Id,
                Name = s.Name,
                Code = s.Code,
                Credits = s.Credits,
                TeacherId = s.TeacherId,
                TeacherName = s.Teacher != null 
                    ? (s.Teacher.FirstName ?? string.Empty) + " " + (s.Teacher.LastName ?? string.Empty) 
                    : "Unassigned"
            })
            .OrderBy(s => s.Code)
            .ToListAsync();

        return View(subjects);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateTeacherDropdown(null);
        return View(new SubjectViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SubjectViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateTeacherDropdown(null);
            return View(model);
        }

        // Verify teacher exists if provided
        if (!string.IsNullOrWhiteSpace(model.TeacherId))
        {
            var teacher = await _userManager.FindByIdAsync(model.TeacherId);
            if (teacher == null || !await _userManager.IsInRoleAsync(teacher, "Teacher"))
            {
                ModelState.AddModelError(nameof(model.TeacherId), "Invalid teacher.");
                await PopulateTeacherDropdown(null);
                return View(model);
            }
        }

        // Check for duplicate code
        var exists = await _db.Subjects.AnyAsync(s => s.Code == model.Code);
        if (exists)
        {
            ModelState.AddModelError(nameof(model.Code), "Subject code already exists.");
            await PopulateTeacherDropdown(null);
            return View(model);
        }

        var subject = new Subject
        {
            Name = model.Name,
            Code = model.Code,
            Credits = model.Credits,
            TeacherId = string.IsNullOrWhiteSpace(model.TeacherId) ? null : model.TeacherId
        };

        _db.Subjects.Add(subject);
        await _db.SaveChangesAsync();

        TempData["StatusMessage"] = "Subject created.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var subject = await _db.Subjects
            .Include(s => s.Teacher)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (subject == null)
        {
            return NotFound();
        }

        var model = new SubjectViewModel
        {
            Id = subject.Id,
            Name = subject.Name,
            Code = subject.Code,
            Credits = subject.Credits,
            TeacherId = subject.TeacherId,
            TeacherName = subject.Teacher != null 
                ? (subject.Teacher.FirstName ?? string.Empty) + " " + (subject.Teacher.LastName ?? string.Empty)
                : "Unassigned"
        };

        await PopulateTeacherDropdown(subject.TeacherId);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, SubjectViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await PopulateTeacherDropdown(model.TeacherId);
            return View(model);
        }

        var subject = await _db.Subjects.FindAsync(id);
        if (subject == null)
        {
            return NotFound();
        }

        // Verify teacher exists if provided
        if (!string.IsNullOrWhiteSpace(model.TeacherId) && model.TeacherId != subject.TeacherId)
        {
            var teacher = await _userManager.FindByIdAsync(model.TeacherId);
            if (teacher == null || !await _userManager.IsInRoleAsync(teacher, "Teacher"))
            {
                ModelState.AddModelError(nameof(model.TeacherId), "Invalid teacher.");
                await PopulateTeacherDropdown(subject.TeacherId);
                return View(model);
            }
        }

        // Check for duplicate code if changed
        if (model.Code != subject.Code)
        {
            var exists = await _db.Subjects.AnyAsync(s => s.Code == model.Code && s.Id != id);
            if (exists)
            {
                ModelState.AddModelError(nameof(model.Code), "Subject code already exists.");
                await PopulateTeacherDropdown(model.TeacherId);
                return View(model);
            }
        }

        subject.Name = model.Name;
        subject.Code = model.Code;
        subject.Credits = model.Credits;
        subject.TeacherId = string.IsNullOrWhiteSpace(model.TeacherId) ? null : model.TeacherId;

        await _db.SaveChangesAsync();

        TempData["StatusMessage"] = "Subject updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var subject = await _db.Subjects.FindAsync(id);
        if (subject == null)
        {
            return NotFound();
        }

        _db.Subjects.Remove(subject);
        await _db.SaveChangesAsync();

        TempData["StatusMessage"] = "Subject deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateTeacherDropdown(string? selectedTeacherId)
    {
        var teachers = await _userManager.GetUsersInRoleAsync("Teacher");
        var teacherList = teachers
            .Select(t => new SelectListItem
            {
                Value = t.Id,
                Text = $"{t.FirstName} {t.LastName}",
                Selected = t.Id == selectedTeacherId
            })
            .OrderBy(x => x.Text)
            .ToList();

        teacherList.Insert(0, new SelectListItem { Value = string.Empty, Text = "-- Unassigned --" });

        ViewBag.Teachers = teacherList;
    }
}
