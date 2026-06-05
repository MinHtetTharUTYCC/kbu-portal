using System.ComponentModel.DataAnnotations;

namespace kbu_portal.ViewModels.Announcements;

public class AnnouncementViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    public bool IsPinned { get; set; }

    [Required]
    public string TargetRole { get; set; } = "All";

    public DateTime CreatedAt { get; set; }

    public string CreatedByName { get; set; } = string.Empty;

    public string CreatedByEmail { get; set; } = string.Empty;
}
