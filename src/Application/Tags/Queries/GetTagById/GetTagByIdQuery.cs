using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Tags.Queries.GetTagById;

public record GetTagByIdQuery(Guid Id) : IRequest<Result<TagBriefDto?>>;

public class GetTagByIdQueryHandler : IRequestHandler<GetTagByIdQuery, Result<TagBriefDto?>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetTagByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<TagBriefDto?>> Handle(GetTagByIdQuery request, CancellationToken cancellationToken)
    {
        var tag = await _context.Tags
            .Where(c => c.Id == request.Id)
            .ProjectTo<TagBriefDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);
            
        return Result<TagBriefDto?>.Success(tag);
    }
}
