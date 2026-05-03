using AskAMuslimBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AskAMuslimBackend.Infrastructure.Data.Configurations;

public class AchievementConfiguration : IEntityTypeConfiguration<Achievement>
{
    public void Configure(EntityTypeBuilder<Achievement> builder)
    {
        builder.ToTable("Achievements");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.HijriYear)
            .IsRequired();

        builder.Property(a => a.HijriMonth)
            .IsRequired();

        builder.Property(a => a.NewMuslims)
            .IsRequired();

        builder.Property(a => a.Dialogues)
            .IsRequired();

        builder.Property(a => a.CoursesCompleted)
            .IsRequired();

        builder.Property(a => a.ActiveVolunteers)
            .IsRequired();

        builder.Property(a => a.SocialMediaReach)
            .IsRequired();

        builder.Property(a => a.EventsHeld)
            .IsRequired();

        builder.HasIndex(a => new { a.HijriYear, a.HijriMonth })
            .IsUnique();
    }
}
