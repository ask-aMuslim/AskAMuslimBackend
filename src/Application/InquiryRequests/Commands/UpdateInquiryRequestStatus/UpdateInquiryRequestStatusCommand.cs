using System.Text.Json.Serialization;
using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.InquiryRequests.Commands.UpdateInquiryRequestStatus;

public record UpdateInquiryRequestStatusCommand : IRequest<Result>
{
    [JsonIgnore]  // Set from route parameter, not request body
    public Guid Id { get; init; }
    public MeetingInquiryRequestStatus Status { get; init; }
}

public class UpdateInquiryRequestStatusCommandHandler : IRequestHandler<UpdateInquiryRequestStatusCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public UpdateInquiryRequestStatusCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<Result> Handle(UpdateInquiryRequestStatusCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.InquiryRequests.FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);
        if (entity == null) return Result.Failure(new[] { "Inquiry request not found" });

        var currentUserId = _user.Id;
        if (currentUserId == null) return Result.Failure(new[] { "Unauthorized" });

        var guidUser = Guid.Parse(currentUserId);
        var isRequester = guidUser == entity.RequesterId;
        var isModerator = entity.ModeratorId.HasValue && guidUser == entity.ModeratorId.Value;
        
        // Check if user is an administrator
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == guidUser, cancellationToken);
        var isAdmin = user != null && user.Role == UserRole.Admin;

        var acceptingWithoutAssignedModerator = request.Status == MeetingInquiryRequestStatus.Accepted && !entity.ModeratorId.HasValue;
        if (acceptingWithoutAssignedModerator)
        {
            var isModeratorRole = user != null && (user.Role == UserRole.Admin || user.Role == UserRole.Instructor || user.Role == UserRole.Preacher);
            if (!isModeratorRole)
            {
                return Result.Failure(new[] { "Only moderators can accept unassigned inquiry requests." });
            }
            entity.ModeratorId = guidUser;
            entity.Status = MeetingInquiryRequestStatus.Accepted;
            await _context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        var allowed = request.Status switch
        {
            MeetingInquiryRequestStatus.Accepted => isModerator || isAdmin,
            MeetingInquiryRequestStatus.Rejected => isModerator || isAdmin,
            MeetingInquiryRequestStatus.Cancelled => isRequester || isModerator || isAdmin,
            MeetingInquiryRequestStatus.Completed => isModerator || isAdmin,
            MeetingInquiryRequestStatus.Pending => false,
            _ => false
        };
        if (!allowed) return Result.Failure(new[] { "Not authorised to set this status." });

        entity.Status = request.Status;
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public class UpdateInquiryRequestStatusCommandValidator : AbstractValidator<UpdateInquiryRequestStatusCommand>
{
    public UpdateInquiryRequestStatusCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Status).IsInEnum();
    }
}
