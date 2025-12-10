using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.QAs.Queries.GetQAById;

public record GetQAByIdQuery(Guid Id) : IRequest<Result<QADetailDto?>>;

public class GetQAByIdQueryHandler : IRequestHandler<GetQAByIdQuery, Result<QADetailDto?>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetQAByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<QADetailDto?>> Handle(GetQAByIdQuery request, CancellationToken cancellationToken)
    {
        var qa = await _context.QAs
            .AsNoTracking()
            .Where(q => q.Id == request.Id)
            .ProjectTo<QADetailDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);
        
        return Result<QADetailDto?>.Success(qa);
    }
}
