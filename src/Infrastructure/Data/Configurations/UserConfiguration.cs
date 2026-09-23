using System.Text.Json;
using System.Text.Json.Serialization;
using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AskAMuslimBackend.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    private static readonly JsonSerializerOptions LanguageJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.PasswordHash)
            .IsRequired();

        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.Gender)
            .HasMaxLength(30);

        builder.Property(u => u.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(u => u.RefreshToken)
            .HasMaxLength(500);

        builder.Property(u => u.FcmToken)
            .HasMaxLength(255);

        builder.Property(u => u.EmailConfirmed)
            .HasDefaultValue(false);

        builder.Property(u => u.IsActive)
            .HasDefaultValue(true);

        builder.Property(u => u.CreatedAt)
            .HasDefaultValueSql("timezone('utc', now())");

        var languagesComparer = new ValueComparer<List<Language>>(
            (c1, c2) => ReferenceEquals(c1, c2) ||
                        (c1 != null && c2 != null &&
                         c1.Count == c2.Count &&
                         c1.OrderBy(x => x).SequenceEqual(c2.OrderBy(x => x))),
            c => c == null
                ? 0
                : c.OrderBy(x => x).Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c == null ? new List<Language>() : c.ToList());

        builder.Property(u => u.Languages)
            .HasConversion(
                v => JsonSerializer.Serialize(v ?? new List<Language>(), LanguageJsonOptions),
                v => string.IsNullOrWhiteSpace(v) ? new List<Language>() :
                     JsonSerializer.Deserialize<List<Language>>(v, LanguageJsonOptions) ?? new List<Language>())
            .HasColumnName("LanguagesJson")
            .HasColumnType("text")
            .Metadata.SetValueComparer(languagesComparer);
    }
}
