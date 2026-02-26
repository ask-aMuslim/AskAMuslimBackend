using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Domain.Enums;

namespace AskAMuslimBackend.Application.Enrollments.Queries.GetUnCompletedEnrollments;

public record GetUnCompletedEnrollmentsQuery : IRequest<Result<List<EnrollmentDto>>>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public Guid? StudentId { get; set; }
}

public class GetUnCompletedEnrollmentsQueryHandler : IRequestHandler<GetUnCompletedEnrollmentsQuery, Result<List<EnrollmentDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMapper _mapper;
    public GetUnCompletedEnrollmentsQueryHandler(IApplicationDbContext context, IMapper mapper, ICurrentUserService currentUserService)
    {
        _context = context;
        _mapper = mapper;
        _currentUserService = currentUserService;
    }
    public async Task<Result<List<EnrollmentDto>>> Handle(GetUnCompletedEnrollmentsQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        request.StudentId = userId;

        if(userId == null)
            return Result<List<EnrollmentDto>>.Failure(new string[] { "Unauthorized" });
 
        var enrollments = await _context.Enrollments
            .Where(e => e.StudentId == request.StudentId && e.Status == EnrollmentStatus.Active)
            .OrderByDescending(e => e.EnrolledAt)
            .ProjectTo<EnrollmentDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
            
        return Result<List<EnrollmentDto>>.Success(enrollments);
    }
}


