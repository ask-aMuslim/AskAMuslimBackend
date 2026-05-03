using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.Volunteers.Queries.GetVolunteers;

public class VolunteerDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public VolunteerType Type { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? CvUrl { get; init; }
    public DateTime Created { get; init; }
}
