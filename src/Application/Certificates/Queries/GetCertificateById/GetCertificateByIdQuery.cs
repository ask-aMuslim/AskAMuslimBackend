using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Certificates.Queries.GetCertificateById;

public record GetCertificateByIdQuery(Guid Id) : IRequest<Result<CertificateDto>>;

public class GetCertificateByIdQueryHandler : IRequestHandler<GetCertificateByIdQuery, Result<CertificateDto>>
{
    private readonly IApplicationDbContext _context;

    public GetCertificateByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<CertificateDto>> Handle(GetCertificateByIdQuery request, CancellationToken cancellationToken)
    {
        var studentLookup = _context.Students
            .Join(
                _context.Users,
                student => student.UserId,
                user => user.Id,
                (student, user) => new { student.Id, FullName = (user.FirstName + " " + user.LastName).Trim() });

        var certificate = await (
            from c in _context.Certificates.AsNoTracking()
            join s in studentLookup on c.StudentId equals s.Id into sg
            from s in sg.DefaultIfEmpty()
            where c.Id == request.Id
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
            .FirstOrDefaultAsync(cancellationToken);

        if (certificate == null)
        {
            return Result<CertificateDto>.Failure(new[] { "Certificate not found" });
        }

        return Result<CertificateDto>.Success(certificate);
    }
}
