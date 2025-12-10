using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.MeetingRequests.Commands.UpdateMeetingRequestStatus;

public record UpdateMeetingRequestStatusCommand : IRequest<Result>
{
    public Guid Id { get; init; }
    public MeetingInquiryRequestStatus Status { get; init; }
}

public class UpdateMeetingRequestStatusCommandHandler : IRequestHandler<UpdateMeetingRequestStatusCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public UpdateMeetingRequestStatusCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<Result> Handle(UpdateMeetingRequestStatusCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.MeetingRequests.FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);
        if (entity == null) return Result.Failure(new[] { "Meeting request not found" });

        var currentUserId = _user.Id;
        if (currentUserId == null) return Result.Failure(new[] { "Unauthorized" });

        var guidUser = Guid.Parse(currentUserId);
        var isRequester = guidUser == entity.RequesterId;
        var isScholar = entity.ScholarId.HasValue && guidUser == entity.ScholarId.Value;

        // If accepting and no scholar yet, any user with scholar role can accept
        var acceptingWithoutAssignedScholar = request.Status == MeetingInquiryRequestStatus.Accepted && !entity.ScholarId.HasValue;
        if (acceptingWithoutAssignedScholar)
        {
            // Verify user role is Instructor or Preacher
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == guidUser, cancellationToken);
            var isScholarRole = user != null && (user.Role == UserRole.Instructor || user.Role == UserRole.Preacher);
            if (!isScholarRole)
            {
                return Result.Failure(new[] { "Only scholars can accept unassigned meeting requests." });
            }

            entity.ScholarId = guidUser; // Claim the request
            entity.Status = MeetingInquiryRequestStatus.Accepted;
            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        var allowed = request.Status switch
        {
            MeetingInquiryRequestStatus.Accepted => isScholar,
            MeetingInquiryRequestStatus.Rejected => isScholar,
            MeetingInquiryRequestStatus.Cancelled => isRequester || isScholar,
            MeetingInquiryRequestStatus.Completed => isScholar,
            MeetingInquiryRequestStatus.Pending => false,
            _ => false
        };
        if (!allowed) return Result.Failure(new[] { "Not authorised to set this status." });

        entity.Status = request.Status;
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public class UpdateMeetingRequestStatusCommandValidator : AbstractValidator<UpdateMeetingRequestStatusCommand>
{
    public UpdateMeetingRequestStatusCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Status).IsInEnum();
    }
}
