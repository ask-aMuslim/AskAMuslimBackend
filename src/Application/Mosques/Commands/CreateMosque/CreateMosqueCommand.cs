using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Security;
using AskAMuslimBackend.Domain.Constants;
using AskAMuslimBackend.Domain.Entities;

namespace AskAMuslimBackend.Application.Mosques.Commands.CreateMosque;

[Authorize(Policy = Policies.ManageAdmins)]
public record CreateMosqueCommand : IRequest<Result<Guid>>
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Address { get; init; }
    public string? Country { get; init; }
    public string? Governorate { get; init; }
    public string? City { get; init; }
    public string? District { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public string? GoogleMapsUrl { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Email { get; init; }
    public string? WebsiteUrl { get; init; }
    public string? ImamName { get; init; }
    public bool HasWomenPrayerArea { get; init; }
    public bool HasFridayKhutbah { get; init; }
    public string? MainImageUrl { get; init; }
    public bool IsActive { get; init; } = true;
}

public class CreateMosqueCommandHandler : IRequestHandler<CreateMosqueCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _context;

    public CreateMosqueCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<Guid>> Handle(CreateMosqueCommand request, CancellationToken cancellationToken)
    {
        var mosque = new Mosque
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            Address = request.Address,
            Country = request.Country,
            Governorate = request.Governorate,
            City = request.City,
            District = request.District,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            GoogleMapsUrl = request.GoogleMapsUrl,
            PhoneNumber = request.PhoneNumber,
            Email = request.Email,
            WebsiteUrl = request.WebsiteUrl,
            ImamName = request.ImamName,
            HasWomenPrayerArea = request.HasWomenPrayerArea,
            HasFridayKhutbah = request.HasFridayKhutbah,
            MainImageUrl = request.MainImageUrl,
            IsActive = request.IsActive
        };

        _context.Mosques.Add(mosque);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(mosque.Id);
    }
}

public class CreateMosqueCommandValidator : AbstractValidator<CreateMosqueCommand>
{
    public CreateMosqueCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.Address).MaximumLength(500);
        RuleFor(x => x.Country).MaximumLength(100);
        RuleFor(x => x.Governorate).MaximumLength(100);
        RuleFor(x => x.City).MaximumLength(100);
        RuleFor(x => x.District).MaximumLength(100);
        RuleFor(x => x.GoogleMapsUrl).MaximumLength(500);
        RuleFor(x => x.PhoneNumber).MaximumLength(50);
        RuleFor(x => x.Email).MaximumLength(256).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.WebsiteUrl).MaximumLength(500);
        RuleFor(x => x.ImamName).MaximumLength(200);
        RuleFor(x => x.MainImageUrl).MaximumLength(500);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).When(x => x.Latitude.HasValue);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).When(x => x.Longitude.HasValue);
    }
}
