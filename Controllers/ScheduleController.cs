using KbuPortal.Data;
using KbuPortal.Models;
using kbu_portal.ViewModels.Schedules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace kbu_portal.Controllers;

[Authorize]
public class ScheduleController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public ScheduleController(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [Authorize(Roles = "Student")]
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User);

        var schedules = await _db.Schedules
            .Include(s => s.Subject)
            .Include(s => s.Subject.Teacher)
            .AsNoTracking()
            .ToListAsync();

        var studentSubjects = await _db.StudentSubjects
            .Where(ss => ss.StudentId == userId)
            .Select(ss => ss.SubjectId)
            .ToListAsync();

        var studentSchedules = schedules
            .Where(s => studentSubjects.Contains(s.SubjectId))
            .Select(s => new ScheduleItem
            {
                Id = s.Id,
                SubjectCode = s.Subject.Code,
                SubjectName = s.Subject.Name,
                TeacherName = s.Subject.Teacher != null 
                    ? $"{s.Subject.Teacher.FirstName} {s.Subject.Teacher.LastName}".Trim() 
                    : "TBD",
                DayOfWeek = s.DayOfWeek,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                Room = s.Room
            })
            .OrderBy(x => x.DayOfWeek)
            .ThenBy(x => x.StartTime)
            .ToList();

        var model = new WeeklyScheduleViewModel
        {
            Schedule = studentSchedules
        };

        return View(model);
    }
}
