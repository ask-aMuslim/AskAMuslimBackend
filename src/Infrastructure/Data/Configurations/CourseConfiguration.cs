using AskAMuslimBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AskAMuslimBackend.Infrastructure.Data.Configurations;

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable("Courses");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Description)
            .HasMaxLength(2000);

        builder.Property(c => c.ThumbnailUrl)
            .HasMaxLength(500);

        builder.Property(c => c.Rating)
            .HasPrecision(3, 2);

        builder.Property(c => c.Order)
            .HasDefaultValue(0);

        builder.HasIndex(c => c.Order);

        builder.HasOne(c => c.Level)
            .WithMany(l => l.Courses)
            .HasForeignKey(c => c.LevelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(c => c.Language)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue("English");

        // many-to-many via CourseTag
        builder
            .HasMany<CourseTag>(c => c.CourseTags)
            .WithOne(ct => ct.Course!)
            .HasForeignKey(ct => ct.CourseId);
    }
}
