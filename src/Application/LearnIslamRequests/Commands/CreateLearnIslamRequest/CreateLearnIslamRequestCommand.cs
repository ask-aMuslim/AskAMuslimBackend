using AskAMuslimBackend.Domain.Enums;
using AskAMuslimBackend.Application.Common.Models;

namespace AskAMuslimBackend.Application.LearnIslamRequests.Commands.CreateLearnIslamRequest;

public record CreateLearnIslamRequestCommand : IRequest<Result<Guid>>
{
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public Language PreferredLanguage { get; init; }
    public LearnIslamKnowledgeLevel KnowledgeLevel { get; init; }
    public string? Message { get; init; }
    public string? CurrentReligion { get; init; }
}