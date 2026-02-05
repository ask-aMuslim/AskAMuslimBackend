using AskAMuslimBackend.Application.Common.Pagination;
using FluentValidation;

namespace AskAMuslimBackend.Application.MuslimTube.Queries.GetVideosByChannel;

public class GetMuslimTubeVideosByChannelQueryValidator : PaginationQueryValidator<GetMuslimTubeVideosByChannelQuery>
{
    public GetMuslimTubeVideosByChannelQueryValidator() : base()
    {
        RuleFor(x => x.SearchTerm).MaximumLength(200).When(x => !string.IsNullOrWhiteSpace(x.SearchTerm));
        RuleFor(x => x.PublishedBefore).GreaterThan(x => x.PublishedAfter!.Value).When(x => x.PublishedAfter.HasValue && x.PublishedBefore.HasValue);
    }
}
