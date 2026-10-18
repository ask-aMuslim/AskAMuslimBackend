using System.Text.Json.Serialization;
using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.SupportMails.Common;
using AskAMuslimBackend.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace AskAMuslimBackend.Application.SupportMails.Commands.AssignSupportMail;

public record AssignSupportMailCommand : IRequest<Result>
{
    [JsonIgnore]  // Set from route parameter, not request body
    public Guid Id { get; init; }
    public Guid ModeratorId { get; init; }
}

public class AssignSupportMailCommandHandler : IRequestHandler<AssignSupportMailCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly ISender _sender;
    private readonly ILogger<AssignSupportMailCommandHandler> _logger;

    public AssignSupportMailCommandHandler(IApplicationDbContext context, ISender sender, ILogger<AssignSupportMailCommandHandler> logger)
    {
        _context = context;
        _sender = sender;
        _logger = logger;
    }

    public async Task<Result> Handle(AssignSupportMailCommand request, CancellationToken cancellationToken)
    {
        var mail = await _context.SupportMails
            .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);

        if (mail == null)
        {
            return Result.Failure(new[] { "Mail not found" });
        }

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

        if (mail.AssignedModeratorId == request.ModeratorId)
        {
            return Result.Success();
        }

        mail.AssignedModeratorId = request.ModeratorId;
        await _context.SaveChangesAsync(cancellationToken);

        await SupportMailNotifications.NotifyModeratorOfAssignmentAsync(_sender, _logger, mail, request.ModeratorId, cancellationToken);

        return Result.Success();
    }
}

public class AssignSupportMailCommandValidator : AbstractValidator<AssignSupportMailCommand>
{
    public AssignSupportMailCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.ModeratorId).NotEmpty();
    }
}
