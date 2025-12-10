using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace AskAMuslimBackend.Application.Certificates.Queries.GetCertificateById;

public record GetCertificateByIdQuery(Guid Id) : IRequest<Result<CertificateDto>>;

public class GetCertificateByIdQueryHandler : IRequestHandler<GetCertificateByIdQuery, Result<CertificateDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetCertificateByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<CertificateDto>> Handle(GetCertificateByIdQuery request, CancellationToken cancellationToken)
    {
        var certificate = await _context.Certificates
            .Where(c => c.Id == request.Id)
            .ProjectTo<CertificateDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);
            
        if (certificate == null)
        {
            return Result<CertificateDto>.Failure(new[] { "Certificate not found" });
        }
        
        return Result<CertificateDto>.Success(certificate);
    }
}
