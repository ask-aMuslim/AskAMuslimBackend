using System.Text.Json.Serialization;
using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Security;
using AskAMuslimBackend.Domain.Constants;

namespace AskAMuslimBackend.Application.Mosques.Commands.UpdateMosque;

[Authorize(Policy = Policies.ManageAdmins)]
public record UpdateMosqueCommand : IRequest<Result>
{
    [JsonIgnore]
    public Guid Id { get; init; }
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
    public bool IsActive { get; init; }
}

public class UpdateMosqueCommandHandler : IRequestHandler<UpdateMosqueCommand, Result>
{
    private readonly IApplicationDbContext _context;

    public UpdateMosqueCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(UpdateMosqueCommand request, CancellationToken cancellationToken)
    {
        var mosque = await _context.Mosques.FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);
        if (mosque == null)
        {
            return Result.Failure(new[] { "Mosque not found" });
        }

        mosque.Name = request.Name.Trim();
        mosque.Description = request.Description;
        mosque.Address = request.Address;
        mosque.Country = request.Country;
        mosque.Governorate = request.Governorate;
        mosque.City = request.City;
        mosque.District = request.District;
        mosque.Latitude = request.Latitude;
        mosque.Longitude = request.Longitude;
        mosque.GoogleMapsUrl = request.GoogleMapsUrl;
        mosque.PhoneNumber = request.PhoneNumber;
        mosque.Email = request.Email;
        mosque.WebsiteUrl = request.WebsiteUrl;
        mosque.ImamName = request.ImamName;
        mosque.HasWomenPrayerArea = request.HasWomenPrayerArea;
        mosque.HasFridayKhutbah = request.HasFridayKhutbah;
        mosque.MainImageUrl = request.MainImageUrl;
        mosque.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public class UpdateMosqueCommandValidator : AbstractValidator<UpdateMosqueCommand>
{
    public UpdateMosqueCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
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
