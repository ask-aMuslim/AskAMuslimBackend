using System.Text.Json.Serialization;
using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.SupportMails.Common;
using AskAMuslimBackend.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace AskAMuslimBackend.Application.SupportMails.Commands.ReplyToSupportMail;

/// <summary>
/// Staff reply. Allowed for the assigned moderator or users with ManageInquiryRequests.
/// </summary>
public record ReplyToSupportMailCommand : IRequest<Result<Guid>>
{
    [JsonIgnore]  // Set from route parameter, not request body
    public Guid Id { get; init; }
    public string Body { get; init; } = string.Empty;
}

public class ReplyToSupportMailCommandHandler : IRequestHandler<ReplyToSupportMailCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IPermissionService _permissionService;
    private readonly TimeProvider _timeProvider;
    private readonly ISender _sender;
    private readonly ILogger<ReplyToSupportMailCommandHandler> _logger;

    public ReplyToSupportMailCommandHandler(
        IApplicationDbContext context,
        IUser user,
        IPermissionService permissionService,
        TimeProvider timeProvider,
        ISender sender,
        ILogger<ReplyToSupportMailCommandHandler> logger)
    {
        _context = context;
        _user = user;
        _permissionService = permissionService;
        _timeProvider = timeProvider;
        _sender = sender;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(ReplyToSupportMailCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_user.Id))
        {
            return Result<Guid>.Failure(new[] { "Unauthorized" });
        }

        var staffId = Guid.Parse(_user.Id);

        var mail = await _context.SupportMails
            .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);

        if (mail == null ||
            !await SupportMailRules.CanStaffAccessAsync(mail, staffId, Permission.ManageInquiryRequests, _permissionService, cancellationToken))
        {
            return Result<Guid>.Failure(new[] { "Mail not found" });
        }

        var message = SupportMailRules.AddMessage(mail, staffId, SupportMailSenderType.Staff, request.Body, _timeProvider.GetUtcNow());

        _context.SupportMailMessages.Add(message);
        await _context.SaveChangesAsync(cancellationToken);

        await SupportMailNotifications.NotifyRequesterOfReplyAsync(_sender, _logger, mail, cancellationToken);

        return Result<Guid>.Success(message.Id);
    }
}

public class ReplyToSupportMailCommandValidator : AbstractValidator<ReplyToSupportMailCommand>
{
    public ReplyToSupportMailCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Body).NotEmpty().MaximumLength(SupportMailRules.ReplyMaxLength);
    }
}
