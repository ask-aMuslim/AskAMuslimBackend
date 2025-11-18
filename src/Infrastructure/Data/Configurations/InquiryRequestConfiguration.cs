using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AskAMuslimBackend.Infrastructure.Data.Configurations;

public class InquiryRequestConfiguration : IEntityTypeConfiguration<InquiryRequest>
{
    // Reuse JsonSerializerOptions and store enums as strings for readability (reads integers too)
    private static readonly JsonSerializerOptions LanguageJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public void Configure(EntityTypeBuilder<InquiryRequest> builder)
    {
        builder.ToTable("InquiryRequests");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Topic)
            .HasConversion<string>()
            .HasMaxLength(100);

        builder.Property(i => i.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(i => i.Message)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(i => i.Response)
            .HasMaxLength(4000);

        // Store collection of Languages as JSON (order-insensitive comparer)
        var languagesComparer = new ValueComparer<List<Language>>(
            (c1, c2) => ReferenceEquals(c1, c2) ||
                        (c1 != null && c2 != null &&
                         c1.Count == c2.Count &&
                         c1.OrderBy(x => x).SequenceEqual(c2.OrderBy(x => x))),
            c => c == null
                ? 0
                : c.OrderBy(x => x).Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
            c => c == null ? new List<Language>() : c.ToList());

        builder.Property(i => i.Languages)
            .HasConversion(
                v => JsonSerializer.Serialize(v ?? new List<Language>(), LanguageJsonOptions),
                v => string.IsNullOrWhiteSpace(v) ? new List<Language>() :
                     JsonSerializer.Deserialize<List<Language>>(v, LanguageJsonOptions) ?? new List<Language>())
            .HasColumnName("LanguagesJson")
            .HasColumnType("nvarchar(max)")
            .Metadata.SetValueComparer(languagesComparer);

        builder.HasIndex(i => new { i.ScholarId, i.Created });

        builder.HasOne(i => i.Requester)
            .WithMany()
            .HasForeignKey(i => i.RequesterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Scholar)
            .WithMany()
            .HasForeignKey(i => i.ScholarId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
