using AskAMuslimBackend.Domain.Entities;

namespace AskAMuslimBackend.Application.Certificates.Queries;

public class CertificateDto
{
    public Guid Id { get; init; }
    public Guid StudentId { get; init; }
    public Guid LevelId { get; init; }
    public string Url { get; init; } = string.Empty;
    public DateTime IssuedAt { get; init; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<Certificate, CertificateDto>();
        }
    }
}
