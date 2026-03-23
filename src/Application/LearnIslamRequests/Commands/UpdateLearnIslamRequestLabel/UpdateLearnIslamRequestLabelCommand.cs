using AskAMuslimBackend.Application.Common.Models;

namespace AskAMuslimBackend.Application.LearnIslamRequests.Commands.UpdateLearnIslamRequestLabel;

public record UpdateLearnIslamRequestLabelCommand : IRequest<Result<bool>>
{
    public Guid Id { get; init; }
    public string? Label { get; init; }
}