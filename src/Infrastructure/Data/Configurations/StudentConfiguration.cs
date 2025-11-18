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

        builder.HasIndex(s => s.UserId)
            .IsUnique();

        builder.HasOne<User>()
            .WithOne()
            .HasForeignKey<Student>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(s => s.Bio).HasMaxLength(2000);
        builder.Property(s => s.PhoneNumber).HasMaxLength(50);
        builder.Property(s => s.Address).HasMaxLength(500);
        builder.Property(s => s.Country).HasMaxLength(100);
    }
}
