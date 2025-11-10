using AskAMuslimBackend.Application.Common.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Tags.Queries.GetTagById;

public record GetTagByIdQuery(Guid Id) : IRequest<TagBriefDto?>;

public class GetTagByIdQueryHandler : IRequestHandler<GetTagByIdQuery, TagBriefDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetTagByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<TagBriefDto?> Handle(GetTagByIdQuery request, CancellationToken cancellationToken)
    {
        return await _context.Tags
            .Where(c => c.Id == request.Id)
            .ProjectTo<TagBriefDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
