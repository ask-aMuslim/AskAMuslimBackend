using AskAMuslimBackend.Application.Common.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Certificates.Queries.GetCertificateById;

public record GetCertificateByIdQuery(Guid Id) : IRequest<CertificateDto?>;

public class GetCertificateByIdQueryHandler : IRequestHandler<GetCertificateByIdQuery, CertificateDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetCertificateByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<CertificateDto?> Handle(GetCertificateByIdQuery request, CancellationToken cancellationToken)
    {
        return await _context.Certificates
            .Where(c => c.Id == request.Id)
            .ProjectTo<CertificateDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
