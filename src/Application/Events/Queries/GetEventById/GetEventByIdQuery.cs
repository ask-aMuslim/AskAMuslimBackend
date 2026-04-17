using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Events.Queries.GetEventById;

public record GetEventByIdQuery(Guid Id) : IRequest<Result<EventDto>>;

public class GetEventByIdQueryHandler : IRequestHandler<GetEventByIdQuery, Result<EventDto>>
{
    private readonly IApplicationDbContext _context;

    public GetEventByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<EventDto>> Handle(GetEventByIdQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var eventData = await _context.Events
            .AsNoTracking()
            .Where(e => e.Id == request.Id)
            .Select(s => new
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
                TotalRegistrations = s.Registrations.Count
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (eventData == null)
        {
            return Result<EventDto>.Failure(new[] { "Event not found" });
        }

        var eventDto = new EventDto
        {
            Id = eventData.Id,
            Title = eventData.Title,
            Description = eventData.Description,
            SpeakerName = eventData.SpeakerName,
            ImageUrl = eventData.ImageUrl,
            StartDateTime = eventData.StartDateTime,
            EndDateTime = eventData.EndDateTime,
            Status = DetermineEventStatus(eventData.StartDateTime, eventData.EndDateTime, now),
            MeetingLink = eventData.MeetingLink,
            IsPublished = eventData.IsPublished,
            TotalRegistrations = eventData.TotalRegistrations
        };

        return Result<EventDto>.Success(eventDto);
    }

    private static EventStatus DetermineEventStatus(DateTime startDateTime, DateTime endDateTime, DateTime now)
        => now < startDateTime ? EventStatus.Upcoming : now <= endDateTime ? EventStatus.Live : EventStatus.Finished;
}
