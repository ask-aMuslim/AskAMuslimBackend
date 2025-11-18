using AskAMuslimBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AskAMuslimBackend.Infrastructure.Data.Configurations;

public class MuslimTubeChannelConfiguration : IEntityTypeConfiguration<MuslimTubeChannel>
{
    public void Configure(EntityTypeBuilder<MuslimTubeChannel> builder)
    {
        builder.Property(c => c.YouTubeChannelId).HasMaxLength(50).IsRequired();
        builder.HasIndex(c => c.YouTubeChannelId).IsUnique();
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Description).HasMaxLength(2000);
        builder.HasQueryFilter(c => !c.IsDeleted);
    }
}
