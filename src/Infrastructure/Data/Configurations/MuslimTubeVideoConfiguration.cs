using AskAMuslimBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AskAMuslimBackend.Infrastructure.Data.Configurations;

public class MuslimTubeVideoConfiguration : IEntityTypeConfiguration<MuslimTubeVideo>
{
    public void Configure(EntityTypeBuilder<MuslimTubeVideo> builder)
    {
        builder.Property(v => v.YouTubeVideoId).HasMaxLength(20).IsRequired();
        builder.HasIndex(v => v.YouTubeVideoId).IsUnique();
        builder.Property(v => v.Title).HasMaxLength(300).IsRequired();
        builder.Property(v => v.Description).HasMaxLength(5000);
        builder.HasOne(v => v.Channel)
               .WithMany(c => c.Videos)
               .HasForeignKey(v => v.MuslimTubeChannelId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.Property(v => v.Type)
               .HasConversion<int>()
               .IsRequired();

        // Match channel soft-delete filter to avoid EF warning about required principal being filtered
        builder.HasQueryFilter(v => v.Channel != null && !v.Channel.IsDeleted);
    }
}
