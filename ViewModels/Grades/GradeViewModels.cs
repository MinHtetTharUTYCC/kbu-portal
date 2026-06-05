using System.ComponentModel.DataAnnotations;

namespace kbu_portal.ViewModels.Grades;

public class GradeEntryViewModel
{
    public int SubjectId { get; set; }

    [Required]
    public string SubjectCode { get; set; } = string.Empty;

    [Required]
    public string SubjectName { get; set; } = string.Empty;

    public List<StudentGradeItem> StudentGrades { get; set; } = new();
}

public class StudentGradeItem
{
    public string StudentId { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? StudentNumber { get; set; }

    public decimal? Score { get; set; }

    public string LetterGrade { get; set; } = string.Empty;

    public int Semester { get; set; } = 1;

    public int Year { get; set; }
}

public class GradeReportViewModel
{
    public string StudentName { get; set; } = string.Empty;

    public List<SemesterGrades> Semesters { get; set; } = new();
}

public class SemesterGrades
{
    public int Semester { get; set; }

    public int Year { get; set; }

    public List<GradeDetail> Grades { get; set; } = new();

    public decimal GPA { get; set; }

    public decimal TotalCredits { get; set; }
}

public class GradeDetail
{
    public string SubjectCode { get; set; } = string.Empty;

    public string SubjectName { get; set; } = string.Empty;

    public int Credits { get; set; }

    public decimal Score { get; set; }

    public string LetterGrade { get; set; } = string.Empty;
}
