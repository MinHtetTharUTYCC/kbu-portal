using System.ComponentModel.DataAnnotations;

namespace kbu_portal.ViewModels.Account;

public class RegisterViewModel
{
    [Required]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [StringLength(50)]
    public string? StudentId { get; set; }

    [StringLength(100)]
    public string? Faculty { get; set; }

    [StringLength(100)]
    public string? Major { get; set; }
}
