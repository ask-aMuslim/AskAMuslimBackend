using AskAMuslimBackend.Application.Common.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Courses.Queries.GetCourseById;

public record GetCourseByIdQuery(Guid Id) : IRequest<CourseBriefDto?>;

public class GetCourseByIdQueryHandler : IRequestHandler<GetCourseByIdQuery, CourseBriefDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetCourseByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<CourseBriefDto?> Handle(GetCourseByIdQuery request, CancellationToken cancellationToken)
    {
        return await _context.Courses
            .Where(c => c.Id == request.Id)
            .ProjectTo<CourseBriefDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
