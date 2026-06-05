using KbuPortal.Data;
using KbuPortal.Models;
using kbu_portal.ViewModels.Grades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace kbu_portal.Controllers;

[Authorize]
public class GradesController : Controller
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public GradesController(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // Teacher: Enter grades for their subject
    [Authorize(Roles = "Teacher")]
    [HttpGet]
    public async Task<IActionResult> Enter(int subjectId)
    {
        var userId = _userManager.GetUserId(User);
        var subject = await _db.Subjects
            .Include(s => s.StudentSubjects)
            .FirstOrDefaultAsync(s => s.Id == subjectId && s.TeacherId == userId);

        if (subject == null)
        {
            return Forbid();
        }

        var students = await _db.StudentSubjects
            .Where(ss => ss.SubjectId == subjectId)
            .Include(ss => ss.Student)
            .OrderBy(ss => ss.Student.FirstName)
            .ToListAsync();

        var currentYear = DateTime.Now.Year;
        var semester = DateTime.Now.Month >= 6 ? 2 : 1;

        var studentGrades = students.Select(ss =>
        {
            var existingGrade = _db.Grades
                .AsNoTracking()
                .FirstOrDefault(g => g.StudentId == ss.StudentId 
                    && g.SubjectId == subjectId 
                    && g.Semester == semester 
                    && g.Year == currentYear);

            return new StudentGradeItem
            {
                StudentId = ss.StudentId,
                Email = ss.Student.Email ?? string.Empty,
                FullName = $"{ss.Student.FirstName} {ss.Student.LastName}".Trim(),
                StudentNumber = ss.Student.StudentId,
                Score = existingGrade?.Score,
                LetterGrade = existingGrade?.LetterGrade ?? string.Empty,
                Semester = semester,
                Year = currentYear
            };
        }).ToList();

        var model = new GradeEntryViewModel
        {
            SubjectId = subjectId,
            SubjectCode = subject.Code,
            SubjectName = subject.Name,
            StudentGrades = studentGrades
        };

        return View(model);
    }

    [Authorize(Roles = "Teacher")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enter(int subjectId, GradeEntryViewModel model)
    {
        var userId = _userManager.GetUserId(User);
        var subject = await _db.Subjects.FirstOrDefaultAsync(s => s.Id == subjectId && s.TeacherId == userId);

        if (subject == null)
        {
            return Forbid();
        }

        foreach (var item in model.StudentGrades)
        {
            if (item.Score.HasValue && item.Score >= 0 && item.Score <= 100)
            {
                var letterGrade = CalculateLetterGrade(item.Score.Value);
                var existingGrade = await _db.Grades
                    .FirstOrDefaultAsync(g => g.StudentId == item.StudentId 
                        && g.SubjectId == subjectId 
                        && g.Semester == item.Semester 
                        && g.Year == item.Year);

                if (existingGrade != null)
                {
                    existingGrade.Score = item.Score.Value;
                    existingGrade.LetterGrade = letterGrade;
                }
                else
                {
                    _db.Grades.Add(new Grade
                    {
                        StudentId = item.StudentId,
                        SubjectId = subjectId,
                        Score = item.Score.Value,
                        LetterGrade = letterGrade,
                        Semester = item.Semester,
                        Year = item.Year
                    });
                }
            }
        }

        await _db.SaveChangesAsync();

        TempData["StatusMessage"] = "Grades saved.";
        return RedirectToAction("Enter", new { subjectId });
    }

    // Student: View own grades
    [Authorize(Roles = "Student")]
    [HttpGet]
    public async Task<IActionResult> MyGrades()
    {
        var userId = _userManager.GetUserId(User);
        var user = await _userManager.FindByIdAsync(userId);

        var grades = await _db.Grades
            .Where(g => g.StudentId == userId)
            .Include(g => g.Subject)
            .OrderByDescending(g => g.Year)
            .ThenByDescending(g => g.Semester)
            .ToListAsync();

        var semesters = grades
            .GroupBy(g => new { g.Year, g.Semester })
            .Select(group =>
            {
                var gradeList = group.ToList();
                var details = gradeList.Select(g => new GradeDetail
                {
                    SubjectCode = g.Subject.Code,
                    SubjectName = g.Subject.Name,
                    Credits = g.Subject.Credits,
                    Score = g.Score,
                    LetterGrade = g.LetterGrade
                }).ToList();

                var gpa = CalculateGPA(gradeList);
                var totalCredits = gradeList.Sum(g => g.Subject.Credits);

                return new SemesterGrades
                {
                    Semester = group.Key.Semester,
                    Year = group.Key.Year,
                    Grades = details,
                    GPA = gpa,
                    TotalCredits = totalCredits
                };
            })
            .ToList();

        var model = new GradeReportViewModel
        {
            StudentName = $"{user?.FirstName} {user?.LastName}".Trim(),
            Semesters = semesters
        };

        return View(model);
    }

    private string CalculateLetterGrade(decimal score)
    {
        return score >= 90 ? "A"
            : score >= 80 ? "B"
            : score >= 70 ? "C"
            : score >= 60 ? "D"
            : "F";
    }

    private decimal CalculateGPA(List<Grade> grades)
    {
        if (grades.Count == 0) return 0;

        var gradePoints = new Dictionary<string, decimal>
        {
            { "A", 4.0m },
            { "B", 3.0m },
            { "C", 2.0m },
            { "D", 1.0m },
            { "F", 0.0m }
        };

        var totalPoints = 0m;
        var totalCredits = 0;

        foreach (var grade in grades)
        {
            if (gradePoints.ContainsKey(grade.LetterGrade))
            {
                totalPoints += gradePoints[grade.LetterGrade] * grade.Subject.Credits;
                totalCredits += grade.Subject.Credits;
            }
        }

        return totalCredits > 0 ? totalPoints / totalCredits : 0;
    }
}
