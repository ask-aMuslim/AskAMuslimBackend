using AskAMuslimBackend.Application.Common.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Enrollments.Queries.GetEnrollmentsByCourse;

public record GetEnrollmentsByCourseQuery(Guid CourseId) : IRequest<List<EnrollmentDto>>;

public class GetEnrollmentsByCourseQueryHandler : IRequestHandler<GetEnrollmentsByCourseQuery, List<EnrollmentDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetEnrollmentsByCourseQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<EnrollmentDto>> Handle(GetEnrollmentsByCourseQuery request, CancellationToken cancellationToken)
    {
        return await _context.Enrollments
            .Where(e => e.CourseId == request.CourseId)
            .OrderByDescending(e => e.EnrolledAt)
            .ProjectTo<EnrollmentDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
    }
}
