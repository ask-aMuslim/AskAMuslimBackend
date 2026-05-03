using AskAMuslimBackend.Domain.Enums;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.AspNetCore.Http;

namespace AskAMuslimBackend.Application.Volunteers.Commands.CreateVolunteer;

public record CreateVolunteerCommand : IRequest<Result<Guid>>
{
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public VolunteerType Type { get; init; }
    public string Message { get; init; } = string.Empty;
    public IFormFile? CvFile { get; init; }
}
