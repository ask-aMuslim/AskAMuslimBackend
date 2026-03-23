using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.LearnIslamRequests.Queries.GetLearnIslamRequests;

public record GetLearnIslamRequestsQuery : IRequest<Result<List<LearnIslamRequestDto>>>;

public class GetLearnIslamRequestsQueryHandler : IRequestHandler<GetLearnIslamRequestsQuery, Result<List<LearnIslamRequestDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetLearnIslamRequestsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<LearnIslamRequestDto>>> Handle(GetLearnIslamRequestsQuery request, CancellationToken cancellationToken)
    {
        var requests = await _context.LearnIslamRequests
            .OrderByDescending(x => x.Created)
            .Select(x => new LearnIslamRequestDto
            {
                Id = x.Id,
                FullName = x.FullName,
                Email = x.Email,
                Country = x.Country,
                PreferredLanguage = x.PreferredLanguage,
                KnowledgeLevel = x.KnowledgeLevel,
                Message = x.Message,
                CurrentReligion = x.CurrentReligion,
                Status = x.Status,
                Label = x.Label,
                Created = x.Created.DateTime
            })
            .ToListAsync(cancellationToken);

        return Result<List<LearnIslamRequestDto>>.Success(requests);
    }
}