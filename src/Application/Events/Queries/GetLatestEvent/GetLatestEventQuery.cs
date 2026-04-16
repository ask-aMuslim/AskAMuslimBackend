using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Events.Queries;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Events.Queries.GetLatestEvent;

public record GetLatestEventQuery : IRequest<Result<LatestEventDto>>;

public class GetLatestEventQueryHandler : IRequestHandler<GetLatestEventQuery, Result<LatestEventDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetLatestEventQueryHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<LatestEventDto>> Handle(GetLatestEventQuery request, CancellationToken cancellationToken)
    {
        var currentUserId = _currentUserService.UserId;

        var eventDto = await _context.Events
            .AsNoTracking()
            .Where(e => e.IsPublished)
            .OrderByDescending(e => e.StartDateTime)
            .Select(s => new LatestEventDto
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
            return Result<LatestEventDto>.Failure(new[] { "Event not found" });
        }

        return Result<LatestEventDto>.Success(eventDto);
    }
}

public class LatestEventDto
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
