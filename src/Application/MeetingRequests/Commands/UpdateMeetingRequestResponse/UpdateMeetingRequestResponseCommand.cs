using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Security;
using AskAMuslimBackend.Domain.Constants;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.MeetingRequests.Commands.UpdateMeetingRequestResponse;

[Authorize(Roles = Roles.Administrator + ",Admin")] // Support both role names
public record UpdateMeetingRequestResponseCommand : IRequest<Result>
{
    public Guid Id { get; init; }
    public string Response { get; init; } = string.Empty;
}

public class UpdateMeetingRequestResponseCommandHandler : IRequestHandler<UpdateMeetingRequestResponseCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public UpdateMeetingRequestResponseCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(UpdateMeetingRequestResponseCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.MeetingRequests.FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);
        if (entity == null) return Result.Failure(new[] { "Meeting request not found" });

        entity.Response = request.Response;
        await _context.SaveChangesAsync(cancellationToken);
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
