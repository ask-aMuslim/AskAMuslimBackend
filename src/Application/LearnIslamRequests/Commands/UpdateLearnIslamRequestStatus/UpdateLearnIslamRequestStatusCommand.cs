using AskAMuslimBackend.Application.Common.Models;

namespace AskAMuslimBackend.Application.LearnIslamRequests.Commands.UpdateLearnIslamRequestStatus;

public record UpdateLearnIslamRequestStatusCommand : IRequest<Result<bool>>
{
    public Guid Id { get; init; }
    public Domain.Enums.LearnIslamRequestStatus Status { get; init; }
}