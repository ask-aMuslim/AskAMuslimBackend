using AskAMuslimBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AskAMuslimBackend.Infrastructure.Data.Configurations;

public class UserPermissionConfiguration : IEntityTypeConfiguration<UserPermission>
{
    public void Configure(EntityTypeBuilder<UserPermission> builder)
    {
        builder.ToTable("UserPermissions");

        builder.HasKey(up => up.Id);

        builder.HasIndex(up => up.UserId)
            .IsUnique();

        builder.HasOne<User>()
            .WithOne()
            .HasForeignKey<UserPermission>(up => up.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(up => up.Notes).HasMaxLength(1000);
    }
}
