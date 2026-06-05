using KbuPortal.Data;
using kbu_portal.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace kbu_portal.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var students = await _userManager.GetUsersInRoleAsync("Student");
        var teachers = await _userManager.GetUsersInRoleAsync("Teacher");

        var totalSubjects = await _db.Subjects.CountAsync();
        var totalAnnouncements = await _db.Announcements.CountAsync();

        var recentAnnouncements = await _db.Announcements
            .AsNoTracking()
            .Include(a => a.CreatedBy)
            .OrderByDescending(a => a.CreatedAt)
            .Take(5)
            .Select(a => new RecentAnnouncement
            {
                Id = a.Id,
                Title = a.Title,
                CreatedAt = a.CreatedAt,
                CreatedBy = (a.CreatedBy.FirstName ?? string.Empty) + " " + (a.CreatedBy.LastName ?? string.Empty),
                IsPinned = a.IsPinned
            })
            .ToListAsync();

        var topSubjects = await _db.Subjects
            .AsNoTracking()
            .Include(s => s.Teacher)
            .Include(s => s.StudentSubjects)
            .OrderByDescending(s => s.StudentSubjects.Count)
            .Take(5)
            .Select(s => new SubjectSummary
            {
                Id = s.Id,
                Code = s.Code,
                Name = s.Name,
                EnrolledStudents = s.StudentSubjects.Count,
                TeacherName = s.Teacher != null 
                    ? (s.Teacher.FirstName ?? string.Empty) + " " + (s.Teacher.LastName ?? string.Empty) 
                    : "Unassigned"
            })
            .ToListAsync();

        var model = new AdminDashboardViewModel
        {
            TotalStudents = students.Count,
            TotalTeachers = teachers.Count,
            TotalSubjects = totalSubjects,
            TotalAnnouncements = totalAnnouncements,
            RecentAnnouncements = recentAnnouncements,
            TopSubjects = topSubjects
        };

        return View(model);
    }
}
