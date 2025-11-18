using AskAMuslimBackend.Domain.Entities;

namespace AskAMuslimBackend.Application.MuslimTube.Queries;

public class MuslimTubeChannelBriefDto
{
    public Guid Id { get; set; }
    public string YouTubeChannelId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ThumbnailDefaultUrl { get; set; }
    public string? ThumbnailMediumUrl { get; set; }
    public string? ThumbnailHighUrl { get; set; }

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<MuslimTubeChannel, MuslimTubeChannelBriefDto>();
        }
    }
}

public class MuslimTubeVideoBriefDto
{
    public Guid Id { get; set; }
    public string YouTubeVideoId { get; set; } = string.Empty;
    public Guid MuslimTubeChannelId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ThumbnailDefaultUrl { get; set; }
    public string? ThumbnailMediumUrl { get; set; }
    public string? ThumbnailHighUrl { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public string VideoUrl { get; set; } = string.Empty;

    private class Mapping : Profile
    {
        public Mapping()
        {
            CreateMap<MuslimTubeVideo, MuslimTubeVideoBriefDto>();
        }
    }
}
