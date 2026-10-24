using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.SupportMails.Common;

namespace AskAMuslimBackend.Application.SupportMails.Queries.GetSupportMailEligibility;

public record GetSupportMailEligibilityQuery : IRequest<Result<SupportMailEligibilityDto>>;

public class GetSupportMailEligibilityQueryHandler : IRequestHandler<GetSupportMailEligibilityQuery, Result<SupportMailEligibilityDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly TimeProvider _timeProvider;

    public GetSupportMailEligibilityQueryHandler(IApplicationDbContext context, IUser user, TimeProvider timeProvider)
    {
        _context = context;
        _user = user;
        _timeProvider = timeProvider;
    }

    public async Task<Result<SupportMailEligibilityDto>> Handle(GetSupportMailEligibilityQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_user.Id))
        {
            return Result<SupportMailEligibilityDto>.Failure(new[] { "Unauthorized" });
        }

        var requesterId = Guid.Parse(_user.Id);

        var lastCreated = await _context.SupportMails
            .Where(m => m.RequesterId == requesterId)
            .MaxAsync(m => (DateTimeOffset?)m.Created, cancellationToken);

        var nextAllowedAt = lastCreated + SupportMailRules.NewMailCooldown;
        var canCreate = nextAllowedAt == null || nextAllowedAt <= _timeProvider.GetUtcNow();

        return Result<SupportMailEligibilityDto>.Success(new SupportMailEligibilityDto
        {
            CanCreate = canCreate,
            NextAllowedAt = canCreate ? null : nextAllowedAt
        });
    }
}
