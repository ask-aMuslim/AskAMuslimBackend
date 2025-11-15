using AskAMuslimBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AskAMuslimBackend.Infrastructure.Data.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("Students");

        builder.HasKey(s => s.Id);

        // Enforce one Student per User
        builder.HasIndex(s => s.UserId).IsUnique();

        builder.Property(s => s.Bio).HasMaxLength(1000);
        builder.Property(s => s.PhoneNumber).HasMaxLength(20);
        builder.Property(s => s.Address).HasMaxLength(500);
    }
}
