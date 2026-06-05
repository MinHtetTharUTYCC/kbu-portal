using System.ComponentModel.DataAnnotations;

namespace kbu_portal.ViewModels.Profile;

public class ProfileViewModel
{
    public string Id { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [StringLength(50)]
    public string? StudentId { get; set; }

    [StringLength(100)]
    public string? Faculty { get; set; }

    [StringLength(100)]
    public string? Major { get; set; }

    public string? ProfilePhoto { get; set; }

    [DataType(DataType.Upload)]
    public IFormFile? PhotoFile { get; set; }

    public string Role { get; set; } = string.Empty;
}
