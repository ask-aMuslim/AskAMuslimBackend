using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AskAMuslimBackend.Infrastructure.Data.Configurations;

public class MeetingRequestConfiguration : IEntityTypeConfiguration<MeetingRequest>
{
    // Reuse JsonSerializerOptions and store enums as strings for readability
    private static readonly JsonSerializerOptions LanguageJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public void Configure(EntityTypeBuilder<MeetingRequest> builder)
    {
        builder.ToTable("MeetingRequests");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Topic)
            .HasConversion<string>()
            .HasMaxLength(100);

        // Store Languages as JSON
        var languagesComparer = new ValueComparer<List<Language>>(
            (c1, c2) => ReferenceEquals(c1, c2) ||
                        (c1 != null && c2 != null &&
                         c1.Count == c2.Count &&
                         c1.OrderBy(x => x).SequenceEqual(c2.OrderBy(x => x))), // order-insensitive equality
            c => c == null
                ? 0
                : c.OrderBy(x => x).Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())), // order-insensitive hashing
            c => c == null ? new List<Language>() : c.ToList());

        builder.Property(m => m.Languages)
            .HasConversion(
                v => JsonSerializer.Serialize(v ?? new List<Language>(), LanguageJsonOptions),
                v => string.IsNullOrWhiteSpace(v)
                    ? new List<Language>()
                    : JsonSerializer.Deserialize<List<Language>>(v, LanguageJsonOptions) ?? new List<Language>())
            .HasColumnName("LanguagesJson")
            .HasColumnType("nvarchar(max)")
            .Metadata.SetValueComparer(languagesComparer);

        builder.Property(m => m.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(m => m.Message)
            .IsRequired()
            .HasMaxLength(4000);

        // Admin response is optional
        builder.Property(m => m.Response)
            .HasMaxLength(4000);

        builder.HasIndex(m => new { m.ScholarId, m.ScheduledAt });

        builder.HasOne(m => m.Requester)
            .WithMany()
            .HasForeignKey(m => m.RequesterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Scholar)
            .WithMany()
            .HasForeignKey(m => m.ScholarId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
