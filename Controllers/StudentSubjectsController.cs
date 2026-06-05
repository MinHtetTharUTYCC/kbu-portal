using KbuPortal.Data;
using KbuPortal.Models;
using kbu_portal.ViewModels.StudentSubjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace kbu_portal.Controllers;

[Authorize(Roles = "Admin")]
public class StudentSubjectsController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public StudentSubjectsController(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<IActionResult> AssignStudents(int subjectId)
    {
        var subject = await _db.Subjects.FindAsync(subjectId);
        if (subject == null)
        {
            return NotFound();
        }

        // Get all students
        var students = await _userManager.GetUsersInRoleAsync("Student");

        // Get currently assigned students
        var assignedStudentIds = await _db.StudentSubjects
            .Where(ss => ss.SubjectId == subjectId)
            .Select(ss => ss.StudentId)
            .ToListAsync();

        var studentItems = students
            .Select(s => new StudentAssignmentItem
            {
                Id = s.Id,
                Email = s.Email ?? string.Empty,
                FullName = $"{s.FirstName} {s.LastName}".Trim(),
                StudentId = s.StudentId,
                IsAssigned = assignedStudentIds.Contains(s.Id)
            })
            .OrderBy(x => x.FullName)
            .ToList();

        var model = new StudentSubjectAssignmentViewModel
        {
            SubjectId = subjectId,
            SubjectCode = subject.Code,
            SubjectName = subject.Name,
            Students = studentItems
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignStudents(int subjectId, StudentSubjectAssignmentViewModel model)
    {
        var subject = await _db.Subjects.FindAsync(subjectId);
        if (subject == null)
        {
            return NotFound();
        }

        // Get current assignments
        var currentAssignments = await _db.StudentSubjects
            .Where(ss => ss.SubjectId == subjectId)
            .ToListAsync();

        // Get selected student IDs from form
        var selectedStudentIds = model.Students
            .Where(s => s.IsAssigned)
            .Select(s => s.Id)
            .ToList();

        // Remove unselected assignments
        foreach (var assignment in currentAssignments)
        {
            if (!selectedStudentIds.Contains(assignment.StudentId))
            {
                _db.StudentSubjects.Remove(assignment);
            }
        }

        // Add new assignments
        var currentStudentIds = currentAssignments.Select(x => x.StudentId).ToList();
        foreach (var studentId in selectedStudentIds)
        {
            if (!currentStudentIds.Contains(studentId))
            {
                _db.StudentSubjects.Add(new StudentSubject
                {
                    StudentId = studentId,
                    SubjectId = subjectId
                });
            }
        }

        await _db.SaveChangesAsync();

        TempData["StatusMessage"] = "Students assigned.";
        return RedirectToAction("Index", "Subjects");
    }
}
