using System.ComponentModel.DataAnnotations;

namespace KbuPortal.Models;

public class Schedule
{
    public int Id { get; set; }

    public int SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;

    public DayOfWeek DayOfWeek { get; set; }

    public TimeSpan StartTime { get; set; }

    public TimeSpan EndTime { get; set; }

    [Required]
    public string Room { get; set; } = string.Empty;
}
