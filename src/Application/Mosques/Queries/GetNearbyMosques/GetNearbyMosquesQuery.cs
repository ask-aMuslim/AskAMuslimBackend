using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Application.Common.Models;
using AskAMuslimBackend.Application.Common.Pagination;
using AskAMuslimBackend.Application.Mosques.Queries;

namespace AskAMuslimBackend.Application.Mosques.Queries.GetNearbyMosques;

public record GetNearbyMosquesQuery : IRequest<Result<PaginatedList<MosqueDto>>>, IPaginationQuery
{
    public decimal Lat { get; init; }
    public decimal Lng { get; init; }
    public double Radius { get; init; }
    public bool? IsActive { get; init; } = true;
    public bool? IsVerified { get; init; }
    public int? PageNumber { get; init; } = 1;
    public int? PageSize { get; init; } = 10;
    public string? SearchTerm => null;
}

public record GetMosquesInRadiusQuery : IRequest<Result<IReadOnlyCollection<MosqueDto>>>
{
    public decimal Lat { get; init; }
    public decimal Lng { get; init; }
    public double Radius { get; init; }
    public bool? IsActive { get; init; } = true;
    public bool? IsVerified { get; init; }
}

public record GetNearestMosqueQuery : IRequest<Result<MosqueDto>>
{
    public decimal Lat { get; init; }
    public decimal Lng { get; init; }
    public bool? IsActive { get; init; } = true;
    public bool? IsVerified { get; init; }
}

public class GetNearbyMosquesQueryHandler : IRequestHandler<GetNearbyMosquesQuery, Result<PaginatedList<MosqueDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetNearbyMosquesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PaginatedList<MosqueDto>>> Handle(GetNearbyMosquesQuery request, CancellationToken cancellationToken)
    {
        var matches = await _context.Mosques
            .AsNoTracking()
            .ApplyGeoFilters(request.IsActive, request.IsVerified)
            .Where(m => m.Latitude.HasValue && m.Longitude.HasValue)
            .Select(m => new MosqueGeoCandidate
            {
                Id = m.Id,
                Name = m.Name,
                Description = m.Description,
                Address = m.Address,
                Country = m.Country,
                Governorate = m.Governorate,
                City = m.City,
                District = m.District,
                Latitude = m.Latitude!.Value,
                Longitude = m.Longitude!.Value,
                GoogleMapsUrl = m.GoogleMapsUrl,
                PhoneNumber = m.PhoneNumber,
                Email = m.Email,
                WebsiteUrl = m.WebsiteUrl,
                ImamName = m.ImamName,
                HasWomenPrayerArea = m.HasWomenPrayerArea,
                HasFridayKhutbah = m.HasFridayKhutbah,
                MainImageUrl = m.MainImageUrl,
                IsActive = m.IsActive,
                IsVerified = m.IsVerified,
                CreatedAt = m.Created,
                UpdatedAt = m.LastModified
            })
            .ToListAsync(cancellationToken);

        var filtered = matches
            .Select(m => m.ToDtoWithDistance(request.Lat, request.Lng))
            .Where(m => m.DistanceInKm.HasValue && m.DistanceInKm.Value <= request.Radius)
            .OrderBy(m => m.DistanceInKm)
            .ToList();

        var pageNumber = request.PageNumber ?? 1;
        var pageSize = request.PageSize ?? 10;
        var pagedItems = filtered
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var paginated = new PaginatedList<MosqueDto>(pagedItems, filtered.Count, pageNumber, pageSize);
        return Result<PaginatedList<MosqueDto>>.Success(paginated);
    }
}

public class GetMosquesInRadiusQueryHandler : IRequestHandler<GetMosquesInRadiusQuery, Result<IReadOnlyCollection<MosqueDto>>>
{
    private readonly IApplicationDbContext _context;

    public GetMosquesInRadiusQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<IReadOnlyCollection<MosqueDto>>> Handle(GetMosquesInRadiusQuery request, CancellationToken cancellationToken)
    {
        var matches = await _context.Mosques
            .AsNoTracking()
            .ApplyGeoFilters(request.IsActive, request.IsVerified)
            .Where(m => m.Latitude.HasValue && m.Longitude.HasValue)
            .Select(m => new MosqueGeoCandidate
            {
                Id = m.Id,
                Name = m.Name,
                Description = m.Description,
                Address = m.Address,
                Country = m.Country,
                Governorate = m.Governorate,
                City = m.City,
                District = m.District,
                Latitude = m.Latitude!.Value,
                Longitude = m.Longitude!.Value,
                GoogleMapsUrl = m.GoogleMapsUrl,
                PhoneNumber = m.PhoneNumber,
                Email = m.Email,
                WebsiteUrl = m.WebsiteUrl,
                ImamName = m.ImamName,
                HasWomenPrayerArea = m.HasWomenPrayerArea,
                HasFridayKhutbah = m.HasFridayKhutbah,
                MainImageUrl = m.MainImageUrl,
                IsActive = m.IsActive,
                IsVerified = m.IsVerified,
                CreatedAt = m.Created,
                UpdatedAt = m.LastModified
            })
            .ToListAsync(cancellationToken);

        var result = matches
            .Select(m => m.ToDtoWithDistance(request.Lat, request.Lng))
            .Where(m => m.DistanceInKm.HasValue && m.DistanceInKm.Value <= request.Radius)
            .OrderBy(m => m.DistanceInKm)
            .ToList();

        return Result<IReadOnlyCollection<MosqueDto>>.Success(result);
    }
}

public class GetNearestMosqueQueryHandler : IRequestHandler<GetNearestMosqueQuery, Result<MosqueDto>>
{
    private readonly IApplicationDbContext _context;

    public GetNearestMosqueQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<MosqueDto>> Handle(GetNearestMosqueQuery request, CancellationToken cancellationToken)
    {
        var matches = await _context.Mosques
            .AsNoTracking()
            .ApplyGeoFilters(request.IsActive, request.IsVerified)
            .Where(m => m.Latitude.HasValue && m.Longitude.HasValue)
            .Select(m => new MosqueGeoCandidate
            {
                Id = m.Id,
                Name = m.Name,
                Description = m.Description,
                Address = m.Address,
                Country = m.Country,
                Governorate = m.Governorate,
                City = m.City,
                District = m.District,
                Latitude = m.Latitude!.Value,
                Longitude = m.Longitude!.Value,
                GoogleMapsUrl = m.GoogleMapsUrl,
                PhoneNumber = m.PhoneNumber,
                Email = m.Email,
                WebsiteUrl = m.WebsiteUrl,
                ImamName = m.ImamName,
                HasWomenPrayerArea = m.HasWomenPrayerArea,
                HasFridayKhutbah = m.HasFridayKhutbah,
                MainImageUrl = m.MainImageUrl,
                IsActive = m.IsActive,
                IsVerified = m.IsVerified,
                CreatedAt = m.Created,
                UpdatedAt = m.LastModified
            })
            .ToListAsync(cancellationToken);

        var nearest = matches
            .Select(m => m.ToDtoWithDistance(request.Lat, request.Lng))
            .OrderBy(m => m.DistanceInKm)
            .FirstOrDefault();

        if (nearest == null)
        {
            return Result<MosqueDto>.Failure(new[] { "No mosques with coordinates were found" });
        }

        return Result<MosqueDto>.Success(nearest);
    }
}

public class GetNearbyMosquesQueryValidator : AbstractValidator<GetNearbyMosquesQuery>
{
    public GetNearbyMosquesQueryValidator()
    {
        RuleFor(x => x.Lat).InclusiveBetween(-90, 90);
        RuleFor(x => x.Lng).InclusiveBetween(-180, 180);
        RuleFor(x => x.Radius).GreaterThan(0);
    }
}

public class GetMosquesInRadiusQueryValidator : AbstractValidator<GetMosquesInRadiusQuery>
{
    public GetMosquesInRadiusQueryValidator()
    {
        RuleFor(x => x.Lat).InclusiveBetween(-90, 90);
        RuleFor(x => x.Lng).InclusiveBetween(-180, 180);
        RuleFor(x => x.Radius).GreaterThan(0);
    }
}

public class GetNearestMosqueQueryValidator : AbstractValidator<GetNearestMosqueQuery>
{
    public GetNearestMosqueQueryValidator()
    {
        RuleFor(x => x.Lat).InclusiveBetween(-90, 90);
        RuleFor(x => x.Lng).InclusiveBetween(-180, 180);
    }
}

internal sealed class MosqueGeoCandidate
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? Address { get; init; }
    public string? Country { get; init; }
    public string? Governorate { get; init; }
    public string? City { get; init; }
    public string? District { get; init; }
    public decimal Latitude { get; init; }
    public decimal Longitude { get; init; }
    public string? GoogleMapsUrl { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Email { get; init; }
    public string? WebsiteUrl { get; init; }
    public string? ImamName { get; init; }
    public bool HasWomenPrayerArea { get; init; }
    public bool HasFridayKhutbah { get; init; }
    public string? MainImageUrl { get; init; }
    public bool IsActive { get; init; }
    public bool IsVerified { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }

    public MosqueDto ToDtoWithDistance(decimal lat, decimal lng)
    {
        return new MosqueDto
        {
            Id = Id,
            Name = Name,
            Description = Description,
            Address = Address,
            Country = Country,
            Governorate = Governorate,
            City = City,
            District = District,
            Latitude = Latitude,
            Longitude = Longitude,
            GoogleMapsUrl = GoogleMapsUrl,
            PhoneNumber = PhoneNumber,
            Email = Email,
            WebsiteUrl = WebsiteUrl,
            ImamName = ImamName,
            HasWomenPrayerArea = HasWomenPrayerArea,
            HasFridayKhutbah = HasFridayKhutbah,
            MainImageUrl = MainImageUrl,
            IsActive = IsActive,
            IsVerified = IsVerified,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            DistanceInKm = CalculateDistanceInKm(lat, lng, Latitude, Longitude)
        };
    }

    private static double CalculateDistanceInKm(decimal lat1, decimal lng1, decimal lat2, decimal lng2)
    {
        const double earthRadiusKm = 6371;

        var dLat = DegreesToRadians((double)(lat2 - lat1));
        var dLng = DegreesToRadians((double)(lng2 - lng1));
        var startLat = DegreesToRadians((double)lat1);
        var endLat = DegreesToRadians((double)lat2);

        var a = Math.Pow(Math.Sin(dLat / 2), 2) +
                Math.Cos(startLat) * Math.Cos(endLat) * Math.Pow(Math.Sin(dLng / 2), 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusKm * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180d;
}

internal static class MosqueGeoQueryExtensions
{
    public static IQueryable<Domain.Entities.Mosque> ApplyGeoFilters(this IQueryable<Domain.Entities.Mosque> query, bool? isActive, bool? isVerified)
    {
        if (isActive.HasValue)
        {
            query = query.Where(m => m.IsActive == isActive.Value);
        }

        if (isVerified.HasValue)
        {
            query = query.Where(m => m.IsVerified == isVerified.Value);
        }

        return query;
    }
}
