using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.MeetingRequests.Commands.AssignMeetingRequest;

public record AssignMeetingRequestCommand : IRequest<Result>
{
    public Guid Id { get; init; }
    public Guid ModeratorId { get; init; }
}

public class AssignMeetingRequestCommandHandler : IRequestHandler<AssignMeetingRequestCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public AssignMeetingRequestCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(AssignMeetingRequestCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.MeetingRequests
            .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);

        if (entity == null)
        {
            return Result.Failure(new[] { "Meeting request not found" });
        }

        // Verify moderator exists and has appropriate role
        var moderator = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.ModeratorId, cancellationToken);

        if (moderator == null)
        {
            return Result.Failure(new[] { "Moderator not found" });
        }

        if (moderator.Role != UserRole.Instructor && moderator.Role != UserRole.Preacher && moderator.Role != UserRole.Admin)
        {
            return Result.Failure(new[] { "User must be an Instructor, Preacher, or Admin to be assigned as a moderator" });
        }

        entity.ScholarId = request.ModeratorId;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public class AssignMeetingRequestCommandValidator : AbstractValidator<AssignMeetingRequestCommand>
{
    public AssignMeetingRequestCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Meeting request ID is required");

        RuleFor(x => x.ModeratorId)
            .NotEmpty().WithMessage("Moderator ID is required");
    }
}
