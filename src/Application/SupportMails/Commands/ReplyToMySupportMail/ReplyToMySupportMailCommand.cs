using System.Text.Json.Serialization;
using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.SupportMails.Common;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.SupportMails.Commands.ReplyToMySupportMail;

public record ReplyToMySupportMailCommand : IRequest<Result<Guid>>
{
    [JsonIgnore]  // Set from route parameter, not request body
    public Guid Id { get; init; }
    public string Body { get; init; } = string.Empty;
}

public class ReplyToMySupportMailCommandHandler : IRequestHandler<ReplyToMySupportMailCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly TimeProvider _timeProvider;

    public ReplyToMySupportMailCommandHandler(IApplicationDbContext context, IUser user, TimeProvider timeProvider)
    {
        _context = context;
        _user = user;
        _timeProvider = timeProvider;
    }

    public async Task<Result<Guid>> Handle(ReplyToMySupportMailCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_user.Id))
        {
            return Result<Guid>.Failure(new[] { "Unauthorized" });
        }

        var requesterId = Guid.Parse(_user.Id);

        var mail = await _context.SupportMails
            .FirstOrDefaultAsync(m => m.Id == request.Id && m.RequesterId == requesterId, cancellationToken);

        if (mail == null)
        {
            return Result<Guid>.Failure(new[] { "Mail not found" });
        }

        // Replying is allowed at any time; a reply to a closed mail reopens it.
        var now = _timeProvider.GetUtcNow();
        var message = SupportMailRules.AddMessage(mail, requesterId, SupportMailSenderType.Requester, request.Body, now);
        mail.RequesterLastReadAt = now;

        _context.SupportMailMessages.Add(message);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(message.Id);
    }
}

public class ReplyToMySupportMailCommandValidator : AbstractValidator<ReplyToMySupportMailCommand>
{
    public ReplyToMySupportMailCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Body).NotEmpty().MaximumLength(SupportMailRules.ReplyMaxLength);
    }
}
