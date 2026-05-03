using AskAMuslimBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AskAMuslimBackend.Infrastructure.Data.Configurations;

public class VolunteerConfiguration : IEntityTypeConfiguration<Volunteer>
{
    public void Configure(EntityTypeBuilder<Volunteer> builder)
    {
        builder.Property(t => t.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.Email)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.Phone)
            .HasMaxLength(50);

        builder.Property(t => t.Message)
            .HasMaxLength(2000);

        builder.Property(t => t.CvUrl)
            .HasMaxLength(500);
    }
}
