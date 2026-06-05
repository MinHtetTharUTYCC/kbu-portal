using System.ComponentModel.DataAnnotations;

namespace KbuPortal.Models;

public class Subject
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string Code { get; set; } = string.Empty;

    public int Credits { get; set; }

    public string? TeacherId { get; set; }
    public ApplicationUser? Teacher { get; set; }

    public ICollection<StudentSubject> StudentSubjects { get; set; } = new List<StudentSubject>();
    public ICollection<Grade> Grades { get; set; } = new List<Grade>();
}
