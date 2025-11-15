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

        // Enforce one Admin per User
        builder.HasIndex(a => a.UserId).IsUnique();

        builder.Property(a => a.Department).HasMaxLength(200);
        builder.Property(a => a.PhoneNumber).HasMaxLength(20);
    }
}
