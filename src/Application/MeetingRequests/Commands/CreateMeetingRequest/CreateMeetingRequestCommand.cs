using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.MeetingRequests.Commands.CreateMeetingRequest;

public record CreateMeetingRequestCommand : IRequest<Guid>
{
    public MeetingTopic Topic { get; init; }
    public string Message { get; init; } = string.Empty;
    public Language Language { get; init; }
    public DateTime ScheduledAt { get; init; }
}

public class CreateMeetingRequestCommandHandler : IRequestHandler<CreateMeetingRequestCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public CreateMeetingRequestCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<Guid> Handle(CreateMeetingRequestCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_user.Id))
        {
            throw new UnauthorizedAccessException();
        }

        var requesterGuid = Guid.Parse(_user.Id);

        var mr = new MeetingRequest
        {
            RequesterId = requesterGuid,
            Topic = request.Topic,
            Message = request.Message,
            Language = request.Language,
            ScheduledAt = request.ScheduledAt,
            Status = MeetingRequestStatus.Pending,
        };

        _context.MeetingRequests.Add(mr);
        await _context.SaveChangesAsync(cancellationToken);
        return mr.Id;
    }
}

public class CreateMeetingRequestCommandValidator : AbstractValidator<CreateMeetingRequestCommand>
{
    public CreateMeetingRequestCommandValidator()
    {
        RuleFor(x => x.Topic).IsInEnum();
        RuleFor(x => x.Language).IsInEnum();
        RuleFor(x => x.Message).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.ScheduledAt).GreaterThan(DateTime.UtcNow).WithMessage("Scheduled time must be in the future.");
    }
}
