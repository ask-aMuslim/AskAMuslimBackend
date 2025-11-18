using AskAMuslimBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AskAMuslimBackend.Infrastructure.Data.Configurations;

public class AdminNoteConfiguration : IEntityTypeConfiguration<AdminNote>
{
    public void Configure(EntityTypeBuilder<AdminNote> builder)
    {
        builder.ToTable("AdminNotes");

        builder.HasKey(an => an.Id);

        builder.HasOne<Student>()
            .WithMany(s => s.AdminNotes)
            .HasForeignKey(an => an.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(an => an.AdminUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(an => an.Content).HasMaxLength(2000).IsRequired();
    }
}
