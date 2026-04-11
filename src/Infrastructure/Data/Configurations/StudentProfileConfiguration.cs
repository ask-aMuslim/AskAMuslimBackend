using AskAMuslimBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AskAMuslimBackend.Infrastructure.Data.Configurations;

public class StudentProfileConfiguration : IEntityTypeConfiguration<StudentProfile>
{
    public void Configure(EntityTypeBuilder<StudentProfile> builder)
    {
        builder.ToTable("StudentProfiles");

        builder.HasKey(sp => sp.Id);

        builder.HasIndex(sp => sp.StudentId)
            .IsUnique();

        builder.Property(sp => sp.Bio)
            .HasMaxLength(1000);

        builder.Property(sp => sp.PhoneNumber)
            .HasMaxLength(20);

        builder.Property(sp => sp.Address)
            .HasMaxLength(500);

        builder.Property(sp => sp.Country)
            .HasMaxLength(100);

        builder.Property(sp => sp.CountryCode)
            .HasMaxLength(10);

        builder.Property(sp => sp.OldReligion)
            .HasMaxLength(100);

        builder.Property(sp => sp.ReasonOfReligion)
            .HasMaxLength(1000);

        builder.Property(sp => sp.ReasonForConversion)
            .HasMaxLength(2000);
    }
}
