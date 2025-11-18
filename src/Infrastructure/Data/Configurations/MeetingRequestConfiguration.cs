using AskAMuslimBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AskAMuslimBackend.Infrastructure.Data.Configurations;

public class MeetingRequestConfiguration : IEntityTypeConfiguration<MeetingRequest>
{
    public void Configure(EntityTypeBuilder<MeetingRequest> builder)
    {
        builder.ToTable("MeetingRequests");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Topic)
            .HasConversion<string>()
            .HasMaxLength(100);

        builder.Property(m => m.Language)
            .HasConversion<string>()
            .HasMaxLength(50);

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
