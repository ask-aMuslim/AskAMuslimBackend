using AskAMuslimBackend.Application.Common.Interfaces;

namespace AskAMuslimBackend.Application.MuslimTube.Commands.ResyncChannel;

public record ResyncMuslimTubeChannelCommand(Guid ChannelId, int MaxVideos = 20) : IRequest<int>;

public class ResyncMuslimTubeChannelCommandValidator : AbstractValidator<ResyncMuslimTubeChannelCommand>
{
    public ResyncMuslimTubeChannelCommandValidator()
    {
        RuleFor(x => x.ChannelId).NotEmpty();
        RuleFor(x => x.MaxVideos).GreaterThan(0).LessThanOrEqualTo(50);
    }
}

public class ResyncMuslimTubeChannelCommandHandler : IRequestHandler<ResyncMuslimTubeChannelCommand, int>
{
    private readonly IApplicationDbContext _context;
    private readonly IYouTubeService _youTube;

    public ResyncMuslimTubeChannelCommandHandler(IApplicationDbContext context, IYouTubeService youTube)
    {
        _context = context;
        _youTube = youTube;
    }

    public async Task<int> Handle(ResyncMuslimTubeChannelCommand request, CancellationToken cancellationToken)
    {
        var channel = await _context.MuslimTubeChannels.FindAsync(new object[] { request.ChannelId }, cancellationToken);
        if (channel == null || channel.IsDeleted)
        {
            return 0;
        }

        var videos = await _youTube.FetchChannelVideosAsync(channel.YouTubeChannelId, request.MaxVideos, cancellationToken);
        int added = 0;
        var existingIds = _context.MuslimTubeVideos.Where(v => v.MuslimTubeChannelId == channel.Id).Select(v => v.YouTubeVideoId).ToHashSet();
        foreach (var v in videos)
        {
            if (existingIds.Contains(v.YouTubeVideoId)) continue;
            _context.MuslimTubeVideos.Add(new Domain.Entities.MuslimTubeVideo
            {
                MuslimTubeChannelId = channel.Id,
                YouTubeVideoId = v.YouTubeVideoId,
                Title = v.Title,
                Description = v.Description,
                ThumbnailDefaultUrl = v.ThumbnailDefaultUrl,
                ThumbnailMediumUrl = v.ThumbnailMediumUrl,
                ThumbnailHighUrl = v.ThumbnailHighUrl,
                PublishedAt = v.PublishedAt,
            });
            added++;
        }
        channel.LastSyncedAt = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return added;
    }
}
