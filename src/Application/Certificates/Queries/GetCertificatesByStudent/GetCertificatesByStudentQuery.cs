using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Certificates.Queries.GetCertificatesByStudent;

public record GetCertificatesByStudentQuery(Guid StudentId) : IRequest<Result<List<CertificateDto>>>;

public class GetCertificatesByStudentQueryHandler : IRequestHandler<GetCertificatesByStudentQuery, Result<List<CertificateDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetCertificatesByStudentQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<CertificateDto>>> Handle(GetCertificatesByStudentQuery request, CancellationToken cancellationToken)
    {
        var studentLookup = _context.Students
            .Join(
                _context.Users,
                student => student.UserId,
                user => user.Id,
                (student, user) => new { student.Id, FullName = (user.FirstName + " " + user.LastName).Trim() });

        var certificates = await (
            from c in _context.Certificates.AsNoTracking()
            join s in studentLookup on c.StudentId equals s.Id into sg
            from s in sg.DefaultIfEmpty()
            where c.StudentId == request.StudentId
            orderby c.IssuedAt descending
            select new CertificateDto
            {
                Id = c.Id,
                StudentId = c.StudentId,
                LevelId = c.LevelId,
                CourseId = c.CourseId,
                Url = c.Url,
                IssuedAt = c.IssuedAt,
                StudentFullName = s != null ? s.FullName : null,
                LevelName = c.Level != null ? c.Level.Title : null,
                CourseName = c.Course != null ? c.Course.Title : null
            })
            .ToListAsync(cancellationToken);

        return Result<List<CertificateDto>>.Success(certificates);
    }
}
