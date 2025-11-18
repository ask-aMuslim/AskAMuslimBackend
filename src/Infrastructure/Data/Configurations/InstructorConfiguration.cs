using AskAMuslimBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AskAMuslimBackend.Infrastructure.Data.Configurations;

public class InstructorConfiguration : IEntityTypeConfiguration<Instructor>
{
    public void Configure(EntityTypeBuilder<Instructor> builder)
    {
        builder.ToTable("Instructors");

        builder.HasKey(i => i.Id);

        builder.HasIndex(i => i.UserId)
            .IsUnique();

        builder.HasOne<User>()
            .WithOne()
            .HasForeignKey<Instructor>(i => i.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(i => i.Rating)
            .HasPrecision(3, 2); // Allows values like 4.75, 5.00

        builder.Property(i => i.Bio).HasMaxLength(2000);
        builder.Property(i => i.Specialization).HasMaxLength(200);
        builder.Property(i => i.PhoneNumber).HasMaxLength(50);
        builder.Property(i => i.Website).HasMaxLength(500);
        builder.Property(i => i.LinkedInProfile).HasMaxLength(500);
    }
}
