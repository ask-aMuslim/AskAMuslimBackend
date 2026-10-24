using System.Text.Json.Serialization;
using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.MeetingRequests.Common;
using AskAMuslimBackend.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.MeetingRequests.Commands.CancelMyMeetingRequest;

/// <summary>Lets a requester cancel their own pending or accepted meeting request.</summary>
public record CancelMyMeetingRequestCommand : IRequest<Result>
{
    [JsonIgnore]  // Set from route parameter, not request body
    public Guid Id { get; init; }
}

public class CancelMyMeetingRequestCommandHandler : IRequestHandler<CancelMyMeetingRequestCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IMeetingRequestNotifier _notifier;

    public CancelMyMeetingRequestCommandHandler(IApplicationDbContext context, IUser user, IMeetingRequestNotifier notifier)
    {
        _context = context;
        _user = user;
        _notifier = notifier;
    }

    public async Task<Result> Handle(CancelMyMeetingRequestCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_user.Id))
        {
            return Result.Failure(new[] { "Unauthorized" });
        }

        var userId = Guid.Parse(_user.Id);

        // Someone else's request looks the same as a missing one.
        var entity = await _context.MeetingRequests
            .FirstOrDefaultAsync(m => m.Id == request.Id && m.RequesterId == userId, cancellationToken);
        if (entity == null)
        {
            return Result.Failure(new[] { "Meeting request not found" });
        }

        if (entity.Status is not (MeetingInquiryRequestStatus.Pending or MeetingInquiryRequestStatus.Accepted))
        {
            return Result.Failure(new[] { "Only pending or accepted meeting requests can be cancelled." });
        }

        entity.Status = MeetingInquiryRequestStatus.Cancelled;
        await _context.SaveChangesAsync(cancellationToken);

        _notifier.StatusChanged(entity.Id, MeetingInquiryRequestStatus.Cancelled, userId);

        return Result.Success();
    }
}

public class CancelMyMeetingRequestCommandValidator : AbstractValidator<CancelMyMeetingRequestCommand>
{
    public CancelMyMeetingRequestCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
