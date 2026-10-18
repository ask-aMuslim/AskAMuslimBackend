using System.Text.RegularExpressions;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.SupportMails.Queries.GetSupportMailTopics;

public record SupportMailTopicDto(MeetingInquiryTopic Value, string Key, string Name);

public record GetSupportMailTopicsQuery : IRequest<Result<List<SupportMailTopicDto>>>;

public class GetSupportMailTopicsQueryHandler : IRequestHandler<GetSupportMailTopicsQuery, Result<List<SupportMailTopicDto>>>
{
    public Task<Result<List<SupportMailTopicDto>>> Handle(GetSupportMailTopicsQuery request, CancellationToken cancellationToken)
    {
        var topics = Enum.GetValues<MeetingInquiryTopic>()
            .Select(t => new SupportMailTopicDto(t, t.ToString(), Regex.Replace(t.ToString(), "(?<!^)([A-Z])", " $1")))
            .ToList();

        return Task.FromResult(Result<List<SupportMailTopicDto>>.Success(topics));
    }
}
