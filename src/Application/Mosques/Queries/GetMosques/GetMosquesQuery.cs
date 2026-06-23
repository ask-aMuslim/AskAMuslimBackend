using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Mappings;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Pagination;
using AskAMuslimBackend.Application.Mosques.Queries;

namespace AskAMuslimBackend.Application.Mosques.Queries.GetMosques;

public record GetMosquesQuery : IRequest<Result<PaginatedList<MosqueDto>>>, IPaginationQuery
{
    public string? Name { get; init; }
    public string? City { get; init; }
    public string? Country { get; init; }
    public string? Governorate { get; init; }
    public string? District { get; init; }
    public bool? IsActive { get; init; }
    public bool? IsVerified { get; init; }
    public int? PageNumber { get; init; } = 1;
    public int? PageSize { get; init; } = 10;
    public string? SearchTerm => Name;
}

public class GetMosquesQueryHandler : IRequestHandler<GetMosquesQuery, Result<PaginatedList<MosqueDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetMosquesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<MosqueDto>>> Handle(GetMosquesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Mosques
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            var name = request.Name.Trim().ToLower();
            query = query.Where(m => m.Name.ToLower().Contains(name));
        }

        if (!string.IsNullOrWhiteSpace(request.City))
        {
            var city = request.City.Trim().ToLower();
            query = query.Where(m => m.City != null && m.City.ToLower().Contains(city));
        }

        if (!string.IsNullOrWhiteSpace(request.Country))
        {
            var country = request.Country.Trim().ToLower();
            query = query.Where(m => m.Country != null && m.Country.ToLower().Contains(country));
        }

        if (!string.IsNullOrWhiteSpace(request.Governorate))
        {
            var governorate = request.Governorate.Trim().ToLower();
            query = query.Where(m => m.Governorate != null && m.Governorate.ToLower().Contains(governorate));
        }

        if (!string.IsNullOrWhiteSpace(request.District))
        {
            var district = request.District.Trim().ToLower();
            query = query.Where(m => m.District != null && m.District.ToLower().Contains(district));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(m => m.IsActive == request.IsActive.Value);
        }

        if (request.IsVerified.HasValue)
        {
            query = query.Where(m => m.IsVerified == request.IsVerified.Value);
        }

        var mosques = await query
            .OrderBy(m => m.Name)
            .ProjectToMosqueDto()
            .PaginatedListAsync(request.PageNumber ?? 1, request.PageSize ?? 10, cancellationToken);

        return Result<PaginatedList<MosqueDto>>.Success(mosques);
    }
}
