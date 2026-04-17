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
        var utcNow = DateTime.UtcNow;

        var eventDto = await _context.Events
            .AsNoTracking()
            .Where(e => e.Id == request.Id)
            .Select(s => new EventDto
            {
                Id = s.Id,
                Title = s.Title,
                Description = s.Description,
                SpeakerName = s.SpeakerName,
                ImageUrl = s.ImageUrl,
                StartDateTime = s.StartDateTime,
                EndDateTime = s.EndDateTime,
                Status = utcNow < s.StartDateTime ? EventStatus.Upcoming : utcNow <= s.EndDateTime ? EventStatus.Live : EventStatus.Finished,
                MeetingLink = s.MeetingLink,
                IsPublished = s.IsPublished,
                TotalRegistrations = s.Registrations.Count
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (eventDto == null)
        {
            return Result<EventDto>.Failure(new[] { "Event not found" });
        }

        return Result<EventDto>.Success(eventDto);
    }
}
