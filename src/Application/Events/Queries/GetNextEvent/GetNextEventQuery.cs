using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Events.Queries.GetNextEvent;

public record GetNextEventQuery : IRequest<Result<NextEventDto>>;

public class GetNextEventQueryHandler : IRequestHandler<GetNextEventQuery, Result<NextEventDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetNextEventQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<NextEventDto>> Handle(GetNextEventQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;
        var now = DateTime.UtcNow;

        var eventDto = await _context.Events
            .AsNoTracking()
            .Where(e => e.IsPublished && e.StartDateTime >= now)
            .OrderBy(e => e.StartDateTime)
            .Select(s => new NextEventDto
            {
                Id = s.Id,
                Title = s.Title,
                Description = s.Description,
                SpeakerName = s.SpeakerName,
                ImageUrl = s.ImageUrl,
                StartDateTime = s.StartDateTime,
                EndDateTime = s.EndDateTime,
                MeetingLink = s.MeetingLink,
                IsPublished = s.IsPublished,
                TotalRegistrations = s.Registrations.Count,
                IsRegistered = currentUserId.HasValue && s.Registrations.Any(r => r.UserId == currentUserId.Value)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (eventDto == null)
        {
            return Result<NextEventDto>.Failure(new[] { "Event not found" });
        }

        return Result<NextEventDto>.Success(eventDto);
    }
}

public class NextEventDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? SpeakerName { get; init; }
    public string? ImageUrl { get; init; }
    public DateTime StartDateTime { get; init; }
    public DateTime EndDateTime { get; init; }
    public string? MeetingLink { get; init; }
    public bool IsPublished { get; init; }
    public int TotalRegistrations { get; init; }
    public bool IsRegistered { get; init; }
}