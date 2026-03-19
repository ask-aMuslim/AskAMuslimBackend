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
        var certificates = await _context.Certificates
            .Where(c => c.StudentId == request.StudentId)
            .OrderByDescending(c => c.IssuedAt)
            .Select(s => new CertificateDto {
            Id = s.Id,
            StudentId = s.StudentId,
            LevelId = s.LevelId,
            Url = s.Url,
            IssuedAt = s.IssuedAt
    })
            .ToListAsync(cancellationToken);
            
        return Result<List<CertificateDto>>.Success(certificates);
    }
}
