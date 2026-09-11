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
        var certificate = await _context.Certificates
            .Where(c => c.Id == request.Id)
            .Select(s => new CertificateDto {
            Id = s.Id,
            StudentId = s.StudentId,
            LevelId = s.LevelId,
            CourseId = s.CourseId,
            Url = s.Url,
            IssuedAt = s.IssuedAt
    })
            .FirstOrDefaultAsync(cancellationToken);
            
        if (certificate == null)
        {
            return Result<CertificateDto>.Failure(new[] { "Certificate not found" });
        }
        
        return Result<CertificateDto>.Success(certificate);
    }
}
