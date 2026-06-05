using System.ComponentModel.DataAnnotations;

namespace kbu_portal.ViewModels.StudentSubjects;

public class StudentSubjectAssignmentViewModel
{
    public int SubjectId { get; set; }

    [Required]
    public string SubjectCode { get; set; } = string.Empty;

    [Required]
    public string SubjectName { get; set; } = string.Empty;

    public List<StudentAssignmentItem> Students { get; set; } = new();
}

public class StudentAssignmentItem
{
    public string Id { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? StudentId { get; set; }

    public bool IsAssigned { get; set; }
}
