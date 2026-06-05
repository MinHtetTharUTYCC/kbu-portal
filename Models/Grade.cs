using System.ComponentModel.DataAnnotations;

namespace KbuPortal.Models;

public class Grade
{
    public int Id { get; set; }

    [Required]
    public string StudentId { get; set; } = string.Empty;
    public ApplicationUser Student { get; set; } = null!;

    public int SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;

    public decimal Score { get; set; }

    [Required]
    public string LetterGrade { get; set; } = string.Empty;

    public int Semester { get; set; }
    public int Year { get; set; }
}
