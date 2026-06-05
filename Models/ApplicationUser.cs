using Microsoft.AspNetCore.Identity;

namespace KbuPortal.Models;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? StudentId { get; set; }
    public string? Faculty { get; set; }
    public string? Major { get; set; }
    public string? ProfilePhoto { get; set; }
}
