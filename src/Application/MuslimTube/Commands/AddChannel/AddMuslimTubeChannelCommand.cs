using AskAMuslimBackend.Application.Common.Interfaces;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Application.MuslimTube.Queries.Caching; // Added

namespace AskAMuslimBackend.Application.MuslimTube.Commands.AddChannel;

/// <summary>
/// Adds a YouTube channel to MuslimTube by URL or channel ID (e.g. https://youtube.com/@handle or UCxxxxxxxxxxxxxxxxxxxxxx).
/// Optionally fetches an initial page of videos on success.
/// </summary>
public record AddMuslimTubeChannelCommand : IRequest<Guid>
{
    /// <summary>
    /// Full channel URL, handle, or raw channel ID (UC*).
    /// </summary>
    public string ChannelUrlOrId { get; init; } = string.Empty;

    /// <summary>
    /// If true, immediately pulls the latest videos for the channel (up to InitialVideoLimit).
    /// </summary>
    public bool FetchVideosImmediately { get; init; } = true;

    /// <summary>
    /// Max number of videos to fetch when FetchVideosImmediately is enabled (1..50).
    /// </summary>
    public int InitialVideoLimit { get; init; } = 20;
}

public class AddMuslimTubeChannelCommandValidator : AbstractValidator<AddMuslimTubeChannelCommand>
{
    public AddMuslimTubeChannelCommandValidator()
    {
        // Ensure we have some input and keep limits sane for the initial fetch
        RuleFor(x => x.ChannelUrlOrId).NotEmpty().MaximumLength(500);
        RuleFor(x => x.InitialVideoLimit).GreaterThan(0).LessThanOrEqualTo(50);
    }
}

public class AddMuslimTubeChannelCommandHandler : IRequestHandler<AddMuslimTubeChannelCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IYouTubeService _youTube;
    private readonly IMuslimTubeCache _cache; // Added

    public AddMuslimTubeChannelCommandHandler(IApplicationDbContext context, IYouTubeService youTube, IMuslimTubeCache cache)
    {
        _context = context;
        _youTube = youTube;
        _cache = cache; // Added
    }

    public async Task<Guid> Handle(AddMuslimTubeChannelCommand request, CancellationToken cancellationToken)
    {
        // 1) Fast-path: caller already provided the raw channel ID; try to find it directly
        var existing = _context.MuslimTubeChannels.FirstOrDefault(c => c.YouTubeChannelId == request.ChannelUrlOrId);
        if (existing != null)
        {
            // Revive a soft-deleted channel instead of creating a duplicate
            if (existing.IsDeleted)
            {
                existing.IsDeleted = false;
                await _context.SaveChangesAsync(cancellationToken);
                // Invalidate cached channel lists so revived channel appears immediately
                await _cache.InvalidateChannelAsync(existing.Id, cancellationToken);
            }
            return existing.Id;
        }

        // 2) Resolve input to a canonical YouTube channel ID using the API (works for handles/URLs)
        var channelResult = await _youTube.FetchChannelAsync(request.ChannelUrlOrId, cancellationToken);
        if (channelResult == null)
        {
            // The given input could not be resolved to a valid channel
            throw new ValidationException("Unable to resolve YouTube channel.");
        }

        // 3) Prevent duplicates even after resolution (handles/URLs may map to an existing channel)
        existing = _context.MuslimTubeChannels.FirstOrDefault(c => c.YouTubeChannelId == channelResult.YouTubeChannelId);
        if (existing != null)
        {
            if (existing.IsDeleted)
            {
                existing.IsDeleted = false;
                await _context.SaveChangesAsync(cancellationToken);
                await _cache.InvalidateChannelAsync(existing.Id, cancellationToken); // ensure cache refresh
            }
            return existing.Id;
        }

        // 4) Create the channel entity from API results
        var channel = new MuslimTubeChannel
        {
            YouTubeChannelId = channelResult.YouTubeChannelId,
            Name = channelResult.Name,
            Description = channelResult.Description,
            ThumbnailDefaultUrl = channelResult.ThumbnailDefaultUrl,
            ThumbnailMediumUrl = channelResult.ThumbnailMediumUrl,
            ThumbnailHighUrl = channelResult.ThumbnailHighUrl,
            AddedAt = DateTimeOffset.UtcNow,
            LastSyncedAt = DateTimeOffset.UtcNow,
            IsDeleted = false
        };

        _context.MuslimTubeChannels.Add(channel);

        // 5) Optionally seed an initial batch of video records for a better first-time UX
        if (request.FetchVideosImmediately)
        {
            var videos = await _youTube.FetchChannelVideosAsync(channel.YouTubeChannelId, request.InitialVideoLimit, cancellationToken);
            foreach (var v in videos)
            {
                _context.MuslimTubeVideos.Add(new MuslimTubeVideo
                {
                    MuslimTubeChannelId = channel.Id,
                    YouTubeVideoId = v.YouTubeVideoId,
                    Title = v.Title,
                    Description = v.Description,
                    ThumbnailDefaultUrl = v.ThumbnailDefaultUrl,
                    ThumbnailMediumUrl = v.ThumbnailMediumUrl,
                    ThumbnailHighUrl = v.ThumbnailHighUrl,
                    PublishedAt = v.PublishedAt
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Invalidate cached channel lists so the newly added channel appears immediately
        await _cache.InvalidateChannelAsync(channel.Id, cancellationToken);

        return channel.Id;
    }
}
