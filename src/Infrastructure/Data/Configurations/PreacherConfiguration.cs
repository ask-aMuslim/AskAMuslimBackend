using AskAMuslimBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AskAMuslimBackend.Infrastructure.Data.Configurations;

public class PreacherConfiguration : IEntityTypeConfiguration<Preacher>
{
    public void Configure(EntityTypeBuilder<Preacher> builder)
    {
        builder.ToTable("Preachers");

        builder.HasKey(p => p.Id);

        builder.HasIndex(p => p.UserId)
            .IsUnique();

        builder.HasOne<User>()
            .WithOne()
            .HasForeignKey<Preacher>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(p => p.Bio).HasMaxLength(2000);
        builder.Property(p => p.Specialization).HasMaxLength(200);
        builder.Property(p => p.PhoneNumber).HasMaxLength(50);

        // Ensure rating has explicit precision to avoid truncation warnings
        builder.Property(p => p.Rating)
            .HasPrecision(3, 2)
            .HasColumnType("decimal(3,2)");
    }
}
