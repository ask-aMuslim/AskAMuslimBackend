using AskAMuslimBackend.Domain.Entities;
using AskAMuslimBackend.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AskAMuslimBackend.Infrastructure.Data.Configurations;

public class StudentQuestionConfiguration : IEntityTypeConfiguration<StudentQuestion>
{
    public void Configure(EntityTypeBuilder<StudentQuestion> builder)
    {
        builder.ToTable("StudentQuestions");

        builder.HasKey(q => q.Id);

        builder.Property(q => q.QuestionText)
            .IsRequired()
            .HasMaxLength(1000);

        builder.HasOne(q => q.Lesson)
            .WithMany(l => l.StudentQuestions)
            .HasForeignKey(q => q.LessonId)
            .OnDelete(DeleteBehavior.Cascade);

        // FK to ApplicationUser (Student)
        builder
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(q => q.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
