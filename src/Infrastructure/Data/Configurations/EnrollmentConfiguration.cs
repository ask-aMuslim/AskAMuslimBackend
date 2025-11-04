using AskAMuslimBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AskAMuslimBackend.Infrastructure.Data.Configurations;

public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.ToTable("Enrollments");

        // Use the entity Id as the primary key (default) to keep compatibility with related FKs
        builder.HasKey(e => e.Id);

        // Ensure a student can only be enrolled once per course
        builder.HasIndex(e => new { e.StudentId, e.CourseId }).IsUnique();

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.EnrolledAt)
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasOne(e => e.Course)
            .WithMany(c => c.Enrollments)
            .HasForeignKey(e => e.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
