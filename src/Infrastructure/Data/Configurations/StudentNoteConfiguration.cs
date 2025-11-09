using AskAMuslimBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AskAMuslimBackend.Infrastructure.Data.Configurations;

public class StudentNoteConfiguration : IEntityTypeConfiguration<StudentNote>
{
    public void Configure(EntityTypeBuilder<StudentNote> builder)
    {
        builder.ToTable("StudentNotes");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Text)
            .IsRequired()
            .HasMaxLength(2000);

        builder.HasOne(n => n.Lesson)
            .WithMany(l => l.StudentNotes)
            .HasForeignKey(n => n.LessonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(n => n.Student)
            .WithMany(s => s.StudentNotes)
            .HasForeignKey(n => n.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
