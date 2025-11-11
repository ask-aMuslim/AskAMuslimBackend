using AskAMuslimBackend.Application.Common.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.QAs.Queries.GetQAById;

public record GetQAByIdQuery(Guid Id) : IRequest<QADetailDto?>;

public class GetQAByIdQueryHandler : IRequestHandler<GetQAByIdQuery, QADetailDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetQAByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<QADetailDto?> Handle(GetQAByIdQuery request, CancellationToken cancellationToken)
    {
        return await _context.QAs
            .AsNoTracking()
            .Where(q => q.Id == request.Id)
            .ProjectTo<QADetailDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
