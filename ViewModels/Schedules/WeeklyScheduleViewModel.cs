using System.ComponentModel.DataAnnotations;

namespace kbu_portal.ViewModels.Schedules;

public class WeeklyScheduleViewModel
{
    public List<ScheduleItem> Schedule { get; set; } = new();
}

public class ScheduleItem
{
    public int Id { get; set; }

    public string SubjectCode { get; set; } = string.Empty;

    public string SubjectName { get; set; } = string.Empty;

    public string TeacherName { get; set; } = string.Empty;

    public DayOfWeek DayOfWeek { get; set; }

    public TimeSpan StartTime { get; set; }

    public TimeSpan EndTime { get; set; }

    public string Room { get; set; } = string.Empty;
}
