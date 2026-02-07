using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.InquiryRequests.Queries;

public class InquiryRequestDto
{
    public Guid Id { get; init; }
    public Guid RequesterId { get; init; }
    public string RequesterName { get; init; } = string.Empty;
    public string? ModeratorName { get; init; }
    public Guid? ModeratorId { get; init; }
    public MeetingInquiryTopic Topic { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<Language> Languages { get; init; } = new();
    public MeetingInquiryRequestStatus Status { get; init; }
    public string? Response { get; init; }
    public DateTimeOffset Created { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<InquiryRequest, InquiryRequestDto>()
                .ForMember(d => d.Languages, opt => opt.MapFrom(s => s.Languages))
                .ForMember(d => d.RequesterName, opt => opt.MapFrom(s => s.Requester != null ? $"{s.Requester.FirstName} {s.Requester.LastName}" : string.Empty))
                .ForMember(d => d.ModeratorName, opt => opt.MapFrom(s => s.Moderator != null ? $"{s.Moderator.FirstName} {s.Moderator.LastName}" : null));
        }
    }
}
