using AskAMuslimBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AskAMuslimBackend.Infrastructure.Data.Configurations;

public class LevelConfiguration : IEntityTypeConfiguration<Level>
{
    public void Configure(EntityTypeBuilder<Level> builder)
    {
        builder.ToTable("Levels");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Title)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(l => l.Order)
            .HasDefaultValue(0);

        builder.HasIndex(l => l.Order);

        builder.Property(l => l.Difficulty)
            .HasConversion<string>() // 👈 Enum stored as string
            .HasMaxLength(50);

        builder.Property(l => l.IsPublished)
            .HasDefaultValue(false);
    }
}
