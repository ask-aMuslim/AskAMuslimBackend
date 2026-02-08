using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.InquiryRequests.Commands.AssignInquiryRequest;

public record AssignInquiryRequestCommand : IRequest<Result>
{
    public Guid Id { get; init; }
    public Guid ModeratorId { get; init; }
}

public class AssignInquiryRequestCommandHandler : IRequestHandler<AssignInquiryRequestCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public AssignInquiryRequestCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(AssignInquiryRequestCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.InquiryRequests
            .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);

        if (entity == null)
        {
            return Result.Failure(new[] { "Inquiry request not found" });
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

        // Verify that the moderator has a corresponding profile in the appropriate table
        bool hasProfile = moderator.Role switch
        {
            UserRole.Admin => await _context.Admins.AnyAsync(a => a.UserId == request.ModeratorId, cancellationToken),
            UserRole.Instructor => await _context.Instructors.AnyAsync(i => i.UserId == request.ModeratorId, cancellationToken),
            UserRole.Preacher => await _context.Preachers.AnyAsync(p => p.UserId == request.ModeratorId, cancellationToken),
            _ => false
        };

        if (!hasProfile)
        {
            return Result.Failure(new[] { $"Moderator profile not found. Please ensure the {moderator.Role} profile is created before assignment." });
        }

        entity.ModeratorId = request.ModeratorId;
        await _context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

public class AssignInquiryRequestCommandValidator : AbstractValidator<AssignInquiryRequestCommand>
{
    public AssignInquiryRequestCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Inquiry request ID is required");

        RuleFor(x => x.ModeratorId)
            .NotEmpty().WithMessage("Moderator ID is required");
    }
}
