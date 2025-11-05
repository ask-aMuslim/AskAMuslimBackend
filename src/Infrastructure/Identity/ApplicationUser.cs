using AskAMuslimBackend.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace AskAMuslimBackend.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public UserRole Role { get; set; }
}
