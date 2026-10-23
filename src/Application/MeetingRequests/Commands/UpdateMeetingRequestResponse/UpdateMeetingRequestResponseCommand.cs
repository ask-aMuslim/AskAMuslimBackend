using System.Text.Json.Serialization;
using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.MeetingRequests.Common;
using AskAMuslimBackend.Application.Common.Security;
using AskAMuslimBackend.Domain.Constants;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.MeetingRequests.Commands.UpdateMeetingRequestResponse;

[Authorize(Roles = Roles.Administrator + ",Admin")] // Support both role names
public record UpdateMeetingRequestResponseCommand : IRequest<Result>
{
    [JsonIgnore]  // Set from route parameter, not request body
    public Guid Id { get; init; }
    public string Response { get; init; } = string.Empty;
}

public class UpdateMeetingRequestResponseCommandHandler : IRequestHandler<UpdateMeetingRequestResponseCommand, Result>
{
    private readonly IApplicationDbContext _context;
    private readonly IMeetingRequestNotifier _notifier;

    public UpdateMeetingRequestResponseCommandHandler(IApplicationDbContext context, IMeetingRequestNotifier notifier)
    {
        _context = context;
        _notifier = notifier;
    }

    public async Task<Result> Handle(UpdateMeetingRequestResponseCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.MeetingRequests.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (entity == null) return Result.Failure(new[] { "Meeting request not found" });

        var changed = entity.Response != request.Response;
        entity.Response = request.Response;
        await _context.SaveChangesAsync(cancellationToken);

        if (changed)
        {
            _notifier.ResponseAdded(entity.Id);
        }
        return Result.Success();
    }
}

public class UpdateMeetingRequestResponseCommandValidator : AbstractValidator<UpdateMeetingRequestResponseCommand>
{
    public UpdateMeetingRequestResponseCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Response).NotEmpty().MaximumLength(4000);
    }
}
