using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.Languages.Queries.GetLanguages;

public class LanguageDto
{
    public int Value { get; init; }
    public string Name { get; init; } = string.Empty;
}

public record GetLanguagesQuery : IRequest<Result<List<LanguageDto>>>;

public class GetLanguagesQueryHandler : IRequestHandler<GetLanguagesQuery, Result<List<LanguageDto>>>
{
    public Task<Result<List<LanguageDto>>> Handle(GetLanguagesQuery request, CancellationToken cancellationToken)
    {
        var languages = Enum.GetValues<Language>()
            .Select(l => new LanguageDto
            {
                Value = (int)l,
                Name = l.ToString()
            })
            .ToList();

        return Task.FromResult(Result<List<LanguageDto>>.Success(languages));
    }
}
