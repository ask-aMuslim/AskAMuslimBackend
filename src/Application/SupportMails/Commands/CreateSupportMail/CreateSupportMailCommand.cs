using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.SupportMails.Common;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.SupportMails.Commands.CreateSupportMail;

public record CreateSupportMailCommand : IRequest<Result<Guid>>
{
    public MeetingInquiryTopic Topic { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string? Explanation { get; init; }
}

public class CreateSupportMailCommandHandler : IRequestHandler<CreateSupportMailCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly TimeProvider _timeProvider;
    private readonly ISupportMailNotifier _notifier;

    public CreateSupportMailCommandHandler(IApplicationDbContext context, IUser user, TimeProvider timeProvider, ISupportMailNotifier notifier)
    {
        _context = context;
        _user = user;
        _timeProvider = timeProvider;
        _notifier = notifier;
    }

    public async Task<Result<Guid>> Handle(CreateSupportMailCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_user.Id))
        {
            return Result<Guid>.Failure(new[] { "Unauthorized" });
        }

        var requesterId = Guid.Parse(_user.Id);
        var now = _timeProvider.GetUtcNow();

        var lastCreated = await _context.SupportMails
            .Where(m => m.RequesterId == requesterId)
            .MaxAsync(m => (DateTimeOffset?)m.Created, cancellationToken);

        if (lastCreated.HasValue && lastCreated.Value + SupportMailRules.NewMailCooldown > now)
        {
            var nextAllowedAt = lastCreated.Value + SupportMailRules.NewMailCooldown;
            return Result<Guid>.Failure(new[] { $"You can send a new inquiry after {nextAllowedAt:O}." });
        }

        var lastNumber = await _context.SupportMails
            .Where(m => m.RequesterId == requesterId)
            .MaxAsync(m => (int?)m.Number, cancellationToken) ?? 0;

        var mail = new SupportMail
        {
            Number = lastNumber + 1,
            RequesterId = requesterId,
            Topic = request.Topic,
            Subject = request.Subject.Trim(),
        };

        // The first message carries the optional explanation; fall back to the subject so the thread is never empty.
        var body = string.IsNullOrWhiteSpace(request.Explanation) ? request.Subject : request.Explanation;
        SupportMailRules.AddMessage(mail, requesterId, SupportMailSenderType.Requester, body, now);

        _context.SupportMails.Add(mail);
        await _context.SaveChangesAsync(cancellationToken);

        _notifier.MailCreated(mail.Id);

        return Result<Guid>.Success(mail.Id);
    }
}

public class CreateSupportMailCommandValidator : AbstractValidator<CreateSupportMailCommand>
{
    public CreateSupportMailCommandValidator()
    {
        RuleFor(x => x.Topic).IsInEnum();
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(SupportMailRules.SubjectMaxLength);
        RuleFor(x => x.Explanation).MaximumLength(SupportMailRules.ExplanationMaxLength);
    }
}
