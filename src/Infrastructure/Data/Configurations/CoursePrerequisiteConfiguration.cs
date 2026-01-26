using AskAMuslimBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AskAMuslimBackend.Infrastructure.Data.Configurations;

public class CoursePrerequisiteConfiguration : IEntityTypeConfiguration<CoursePrerequisite>
{
    public void Configure(EntityTypeBuilder<CoursePrerequisite> builder)
    {
        builder.ToTable("CoursePrerequisites");

        // Composite primary key
        builder.HasKey(cp => new { cp.CourseId, cp.PrerequisiteCourseId });

        builder.Property(cp => cp.Order)
            .HasDefaultValue(0);

        // Index on PrerequisiteCourseId for reverse lookups (find courses unlocked by a course)
        builder.HasIndex(cp => cp.PrerequisiteCourseId);

        // Configure the relationship from Course to its prerequisites
        builder.HasOne(cp => cp.Course)
            .WithMany(c => c.CoursePrerequisites)
            .HasForeignKey(cp => cp.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure the reverse relationship (courses that have this course as prerequisite)
        builder.HasOne(cp => cp.PrerequisiteCourse)
            .WithMany(c => c.PrerequisiteForCourses)
            .HasForeignKey(cp => cp.PrerequisiteCourseId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent deleting a course that is a prerequisite
    }
}
