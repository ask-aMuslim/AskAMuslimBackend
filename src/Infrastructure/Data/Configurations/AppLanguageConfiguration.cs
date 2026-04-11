using AskAMuslimBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AskAMuslimBackend.Infrastructure.Data.Configurations;

public class AppLanguageConfiguration : IEntityTypeConfiguration<AppLanguage>
{
    public void Configure(EntityTypeBuilder<AppLanguage> builder)
    {
        builder.ToTable("Languages");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(l => l.Code)
            .IsRequired()
            .HasMaxLength(10);

        builder.HasIndex(l => l.Code)
            .IsUnique();

        builder.HasMany(l => l.Users)
            .WithMany(u => u.Languages)
            .UsingEntity<Dictionary<string, object>>(
                "UserLanguages",
                j => j
                    .HasOne<User>()
                    .WithMany()
                    .HasForeignKey("UserId")
                    .OnDelete(DeleteBehavior.Cascade),
                j => j
                    .HasOne<AppLanguage>()
                    .WithMany()
                    .HasForeignKey("LanguageId")
                    .OnDelete(DeleteBehavior.Cascade),
                j =>
                {
                    j.HasKey("UserId", "LanguageId");
                    j.ToTable("UserLanguages");
                });
    }
}
