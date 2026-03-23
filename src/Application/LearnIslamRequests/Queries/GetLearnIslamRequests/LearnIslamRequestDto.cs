using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.LearnIslamRequests.Queries.GetLearnIslamRequests;

public class LearnIslamRequestDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public Language PreferredLanguage { get; init; }
    public LearnIslamKnowledgeLevel KnowledgeLevel { get; init; }
    public string? Message { get; init; }
    public string? CurrentReligion { get; init; }
    public LearnIslamRequestStatus Status { get; init; }
    public string? Label { get; init; }
    public DateTime Created { get; init; }
}