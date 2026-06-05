namespace kbu_portal.ViewModels.Admin;

public class AdminDashboardViewModel
{
    public int TotalStudents { get; set; }

    public int TotalTeachers { get; set; }

    public int TotalSubjects { get; set; }

    public int TotalAnnouncements { get; set; }

    public List<RecentAnnouncement> RecentAnnouncements { get; set; } = new();

    public List<SubjectSummary> TopSubjects { get; set; } = new();
}

public class RecentAnnouncement
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public bool IsPinned { get; set; }
}

public class SubjectSummary
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int EnrolledStudents { get; set; }

    public string TeacherName { get; set; } = string.Empty;
}
