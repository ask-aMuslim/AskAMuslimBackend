using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Enrollments.Queries.GetEnrollmentsByStudent;

public record GetEnrollmentsByStudentQuery(Guid StudentId) : IRequest<Result<List<EnrollmentDto>>>;

public class GetEnrollmentsByStudentQueryHandler : IRequestHandler<GetEnrollmentsByStudentQuery, Result<List<EnrollmentDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetEnrollmentsByStudentQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<List<EnrollmentDto>>> Handle(GetEnrollmentsByStudentQuery request, CancellationToken cancellationToken)
    {
        var enrollments = await _context.Enrollments
            .Where(e => e.StudentId == request.StudentId)
            .OrderByDescending(e => e.EnrolledAt)
            .ProjectTo<EnrollmentDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
            
        return Result<List<EnrollmentDto>>.Success(enrollments);
    }
}
