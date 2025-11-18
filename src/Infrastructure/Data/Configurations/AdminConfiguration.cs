using AskAMuslimBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AskAMuslimBackend.Infrastructure.Data.Configurations;

public class AdminConfiguration : IEntityTypeConfiguration<Admin>
{
    public void Configure(EntityTypeBuilder<Admin> builder)
    {
        builder.ToTable("Admins");

        builder.HasKey(a => a.Id);

        builder.HasIndex(a => a.UserId)
            .IsUnique();

        builder.HasOne<User>()
            .WithOne()
            .HasForeignKey<Admin>(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(a => a.Department).HasMaxLength(200);
        builder.Property(a => a.PhoneNumber).HasMaxLength(50);
    }
}
