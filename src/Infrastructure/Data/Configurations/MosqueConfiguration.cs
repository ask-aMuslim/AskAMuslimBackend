using AskAMuslimBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AskAMuslimBackend.Infrastructure.Data.Configurations;

public class MosqueConfiguration : IEntityTypeConfiguration<Mosque>
{
    public void Configure(EntityTypeBuilder<Mosque> builder)
    {
        builder.ToTable("Mosques");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.Description)
            .HasMaxLength(4000);

        builder.Property(m => m.Address)
            .HasMaxLength(500);

        builder.Property(m => m.Country)
            .HasMaxLength(100);

        builder.Property(m => m.Governorate)
            .HasMaxLength(100);

        builder.Property(m => m.City)
            .HasMaxLength(100);

        builder.Property(m => m.District)
            .HasMaxLength(100);

        builder.Property(m => m.Latitude)
            .HasPrecision(9, 6);

        builder.Property(m => m.Longitude)
            .HasPrecision(9, 6);

        builder.Property(m => m.GoogleMapsUrl)
            .HasMaxLength(500);

        builder.Property(m => m.PhoneNumber)
            .HasMaxLength(50);

        builder.Property(m => m.Email)
            .HasMaxLength(256);

        builder.Property(m => m.WebsiteUrl)
            .HasMaxLength(500);

        builder.Property(m => m.ImamName)
            .HasMaxLength(200);

        builder.Property(m => m.MainImageUrl)
            .HasMaxLength(500);

        builder.Property(m => m.IsActive)
            .HasDefaultValue(true);

        builder.Property(m => m.IsVerified)
            .HasDefaultValue(false);

        builder.HasIndex(m => m.Name);
        builder.HasIndex(m => new { m.Country, m.City });
        builder.HasIndex(m => new { m.IsActive, m.IsVerified });
    }
}
