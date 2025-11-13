using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.MeetingRequests.Queries;

public class MeetingRequestDto
{
    public Guid Id { get; init; }
    public Guid RequesterId { get; init; }
    public Guid? ScholarId { get; init; }
    public MeetingTopic Topic { get; init; }
    public string Message { get; init; } = string.Empty;
    public Language Language { get; init; }
    public DateTime ScheduledAt { get; init; }
    public MeetingRequestStatus Status { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<MeetingRequest, MeetingRequestDto>();
        }
    }
}
