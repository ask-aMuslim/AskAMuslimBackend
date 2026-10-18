using AskAMuslimBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AskAMuslimBackend.Infrastructure.Data.Configurations;

public class SupportMailConfiguration : IEntityTypeConfiguration<SupportMail>
{
    public void Configure(EntityTypeBuilder<SupportMail> builder)
    {
        builder.ToTable("SupportMails");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Topic)
            .HasConversion<string>()
            .HasMaxLength(100);

        builder.Property(m => m.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(m => m.LastMessageSenderType)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(m => m.Subject)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(m => new { m.RequesterId, m.Number }).IsUnique();
        builder.HasIndex(m => new { m.Status, m.LastMessageAt });
        builder.HasIndex(m => new { m.AssignedModeratorId, m.LastMessageAt });

        builder.HasOne(m => m.Requester)
            .WithMany()
            .HasForeignKey(m => m.RequesterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.AssignedModerator)
            .WithMany()
            .HasForeignKey(m => m.AssignedModeratorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(m => m.Messages)
            .WithOne(msg => msg.Mail)
            .HasForeignKey(msg => msg.MailId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SupportMailMessageConfiguration : IEntityTypeConfiguration<SupportMailMessage>
{
    public void Configure(EntityTypeBuilder<SupportMailMessage> builder)
    {
        builder.ToTable("SupportMailMessages");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.SenderType)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(m => m.Body)
            .IsRequired()
            .HasMaxLength(4000);

        builder.HasIndex(m => new { m.MailId, m.Created });

        builder.HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
