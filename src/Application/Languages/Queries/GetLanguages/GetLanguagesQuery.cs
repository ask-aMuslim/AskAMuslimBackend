using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Languages.Queries.GetLanguages;

public class LanguageDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
}

public record GetLanguagesQuery : IRequest<Result<List<LanguageDto>>>;

public class GetLanguagesQueryHandler : IRequestHandler<GetLanguagesQuery, Result<List<LanguageDto>>>
{
    private static readonly (string Name, string Code)[] LanguageSeeds =
    {
        ("English", "en"),
        ("Arabic", "ar"),
        ("French", "fr"),
        ("Spanish", "es"),
        ("German", "de"),
        ("Portuguese", "pt")
    };

    private readonly IApplicationDbContext _context;

    public GetLanguagesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<LanguageDto>>> Handle(GetLanguagesQuery request, CancellationToken cancellationToken)
    {
        var existing = await _context.AppLanguages
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var byCode = existing.ToDictionary(l => l.Code, l => l.Id, StringComparer.OrdinalIgnoreCase);

        var result = LanguageSeeds
            .Select(seed => new LanguageDto
            {
                Id = byCode.TryGetValue(seed.Code, out var id) ? id : 0,
                Name = seed.Name,
                Code = seed.Code
            })
            .ToList();

        return Result<List<LanguageDto>>.Success(result);
    }
}
