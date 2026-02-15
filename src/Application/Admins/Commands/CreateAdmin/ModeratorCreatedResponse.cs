namespace AskAMuslimBackend.Application.Admins.Commands.CreateAdmin;

public class ModeratorCreatedResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string GeneratedPassword { get; set; } = string.Empty;
}
