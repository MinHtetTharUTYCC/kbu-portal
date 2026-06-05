using System.ComponentModel.DataAnnotations;

namespace KbuPortal.Models;

public class Announcement
{
    public int Id { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public string CreatedById { get; set; } = string.Empty;
    public ApplicationUser CreatedBy { get; set; } = null!;

    public bool IsPinned { get; set; }

    [Required]
    public string TargetRole { get; set; } = "All";
}
