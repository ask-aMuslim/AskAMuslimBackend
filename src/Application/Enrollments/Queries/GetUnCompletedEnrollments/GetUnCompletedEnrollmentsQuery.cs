using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.Enrollments.Queries.GetUnCompletedEnrollments;

public record GetUnCompletedEnrollmentsQuery(Guid StudentId) : IRequest<Result<List<EnrollmentDto>>>;

public class GetUnCompletedEnrollmentsQueryHandler : IRequestHandler<GetUnCompletedEnrollmentsQuery, Result<List<EnrollmentDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;
    public GetUnCompletedEnrollmentsQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }
    public async Task<Result<List<EnrollmentDto>>> Handle(GetUnCompletedEnrollmentsQuery request, CancellationToken cancellationToken)
    {
        var enrollments = await _context.Enrollments
            .Where(e => e.StudentId == request.StudentId && e.Status == EnrollmentStatus.Active)
            .OrderByDescending(e => e.EnrolledAt)
            .ProjectTo<EnrollmentDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
            
        return Result<List<EnrollmentDto>>.Success(enrollments);
    }
}


