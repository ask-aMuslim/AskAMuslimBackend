using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Mosques.Queries;

namespace AskAMuslimBackend.Application.Mosques.Queries.GetMosqueById;

public record GetMosqueByIdQuery(Guid Id) : IRequest<Result<MosqueDto>>;

public class GetMosqueByIdQueryHandler : IRequestHandler<GetMosqueByIdQuery, Result<MosqueDto>>
{
    private readonly IApplicationDbContext _context;

    public GetMosqueByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<MosqueDto>> Handle(GetMosqueByIdQuery request, CancellationToken cancellationToken)
    {
        var mosque = await _context.Mosques
            .AsNoTracking()
            .Where(m => m.Id == request.Id)
            .ProjectToMosqueDto()
            .FirstOrDefaultAsync(cancellationToken);

        if (mosque == null)
        {
            return Result<MosqueDto>.Failure(new[] { "Mosque not found" });
        }

        return Result<MosqueDto>.Success(mosque);
    }
}
