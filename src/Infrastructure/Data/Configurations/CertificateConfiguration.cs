using AskAMuslimBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AskAMuslimBackend.Infrastructure.Data.Configurations;

public class CertificateConfiguration : IEntityTypeConfiguration<Certificate>
{
    public void Configure(EntityTypeBuilder<Certificate> builder)
    {
        builder.ToTable("Certificates");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Url)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasOne(c => c.Level)
            .WithOne(l => l.Certificate)
            .HasForeignKey<Certificate>(c => c.LevelId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
