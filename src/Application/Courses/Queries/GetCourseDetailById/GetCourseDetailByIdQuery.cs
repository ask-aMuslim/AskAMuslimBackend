using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;
using AutoMapper;
using AutoMapper.QueryableExtensions;

namespace AskAMuslimBackend.Application.Courses.Queries.GetCourseDetailById;

public record GetCourseDetailByIdQuery(Guid CourseId, Guid? StudentId = null) : IRequest<Result<CourseDetailDto?>>;

public class GetCourseDetailByIdQueryHandler : IRequestHandler<GetCourseDetailByIdQuery, Result<CourseDetailDto?>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetCourseDetailByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<CourseDetailDto?>> Handle(GetCourseDetailByIdQuery request, CancellationToken cancellationToken)
    {
        var course = await _context.Courses
            .Where(c => c.Id == request.CourseId)
            .ProjectTo<CourseDetailDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);

        if (course == null)
        {
            return Result<CourseDetailDto?>.Success(null);
        }

        // If a student ID is provided, check which prerequisites are completed
        if (request.StudentId.HasValue)
        {
            var completedCourseIds = await _context.Enrollments
                .Where(e => e.StudentId == request.StudentId.Value && 
                           e.Status == EnrollmentStatus.Completed)
                .Select(e => e.CourseId)
                .ToListAsync(cancellationToken);

            foreach (var prerequisite in course.Prerequisites)
            {
                prerequisite.IsCompleted = completedCourseIds.Contains(prerequisite.Id);
            }

            foreach (var unlockedCourse in course.UnlockedCourses)
            {
                unlockedCourse.IsCompleted = completedCourseIds.Contains(unlockedCourse.Id);
            }
        }

        return Result<CourseDetailDto?>.Success(course);
    }
}
