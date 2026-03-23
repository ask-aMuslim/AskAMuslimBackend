using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.MeetingRequests.Commands.CreateMeetingRequest;

public record CreateMeetingRequestCommand : IRequest<Result<Guid>>
{
    public MeetingInquiryTopic? Topic { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<Language> Languages { get; init; } = new();
    public DateTime ScheduledAt { get; init; }
}

public class CreateMeetingRequestCommandHandler : IRequestHandler<CreateMeetingRequestCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public CreateMeetingRequestCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<Result<Guid>> Handle(CreateMeetingRequestCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_user.Id))
        {
            return Result<Guid>.Failure(new[] { "Unauthorized" });
        }

        var requesterGuid = Guid.Parse(_user.Id);

        var mr = new MeetingRequest
        {
            RequesterId = requesterGuid,
            Topic = request.Topic ?? MeetingInquiryTopic.GeneralInquiry,
            Message = request.Message,
            Languages = request.Languages,
            ScheduledAt = request.ScheduledAt,
            Status = MeetingInquiryRequestStatus.Pending,
        };

        _context.MeetingRequests.Add(mr);
        await _context.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(mr.Id);
    }
}

public class CreateMeetingRequestCommandValidator : AbstractValidator<CreateMeetingRequestCommand>
{
    public CreateMeetingRequestCommandValidator()
    {
        RuleFor(x => x.Topic).IsInEnum().When(x => x.Topic.HasValue);
        RuleFor(x => x.Message).NotEmpty().MaximumLength(4000);
        RuleFor(x => x.ScheduledAt).GreaterThan(DateTime.UtcNow).WithMessage("Scheduled time must be in the future.");
        RuleFor(x => x.Languages).NotEmpty().WithMessage("At least one language must be provided.");
        RuleForEach(x => x.Languages).IsInEnum();
    }
}
