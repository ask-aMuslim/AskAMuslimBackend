using AskAMuslimBackend.Domain.Common;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Domain.Entities;

public class Volunteer : BaseAuditableEntity
{
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public VolunteerType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? CvUrl { get; set; }
}
