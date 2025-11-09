using AskAMuslimBackend.Application.Common.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Certificates.Queries.GetCertificatesByStudent;

public record GetCertificatesByStudentQuery(Guid StudentId) : IRequest<List<CertificateDto>>;

public class GetCertificatesByStudentQueryHandler : IRequestHandler<GetCertificatesByStudentQuery, List<CertificateDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetCertificatesByStudentQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<List<CertificateDto>> Handle(GetCertificatesByStudentQuery request, CancellationToken cancellationToken)
    {
        return await _context.Certificates
            .Where(c => c.StudentId == request.StudentId)
            .OrderByDescending(c => c.IssuedAt)
            .ProjectTo<CertificateDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
    }
}
