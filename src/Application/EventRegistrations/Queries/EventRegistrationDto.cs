using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.EventRegistrations.Queries;

public class EventRegistrationDto
{
    public Guid Id { get; init; }
    public Guid EventId { get; init; }
    public Guid UserId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? QuestionText { get; init; }
    public EventStatus Status { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<EventRegistration, EventRegistrationDto>()
                .ForMember(d => d.FirstName, opt => opt.MapFrom(s => s.User != null ? s.User.FirstName : string.Empty))
                .ForMember(d => d.LastName, opt => opt.MapFrom(s => s.User != null ? s.User.LastName : string.Empty));
        }
    }
}
