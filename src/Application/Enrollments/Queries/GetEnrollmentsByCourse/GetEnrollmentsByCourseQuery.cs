using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Enrollments.Queries.GetEnrollmentsByCourse;

public record GetEnrollmentsByCourseQuery(Guid CourseId) : IRequest<Result<List<EnrollmentDto>>>;

public class GetEnrollmentsByCourseQueryHandler : IRequestHandler<GetEnrollmentsByCourseQuery, Result<List<EnrollmentDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetEnrollmentsByCourseQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<List<EnrollmentDto>>> Handle(GetEnrollmentsByCourseQuery request, CancellationToken cancellationToken)
    {
        var enrollments = await _context.Enrollments
            .Where(e => e.CourseId == request.CourseId)
            .OrderByDescending(e => e.EnrolledAt)
            .ProjectTo<EnrollmentDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
            
        return Result<List<EnrollmentDto>>.Success(enrollments);
    }
}
