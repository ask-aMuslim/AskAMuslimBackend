using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Certificates.Queries.GetCertificatesByStudent;

public record GetCertificatesByStudentQuery(Guid StudentId) : IRequest<Result<List<CertificateDto>>>;

public class GetCertificatesByStudentQueryHandler : IRequestHandler<GetCertificatesByStudentQuery, Result<List<CertificateDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetCertificatesByStudentQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<List<CertificateDto>>> Handle(GetCertificatesByStudentQuery request, CancellationToken cancellationToken)
    {
        var certificates = await _context.Certificates
            .Where(c => c.StudentId == request.StudentId)
            .OrderByDescending(c => c.IssuedAt)
            .ProjectTo<CertificateDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
            
        return Result<List<CertificateDto>>.Success(certificates);
    }
}
