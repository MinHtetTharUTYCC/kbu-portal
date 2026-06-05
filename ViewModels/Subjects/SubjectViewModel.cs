using System.ComponentModel.DataAnnotations;

namespace kbu_portal.ViewModels.Subjects;

public class SubjectViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string Code { get; set; } = string.Empty;

    [Range(1, 12)]
    public int Credits { get; set; } = 3;

    [StringLength(450)]
    public string? TeacherId { get; set; }

    public string? TeacherName { get; set; }
}
