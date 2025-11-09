using AskAMuslimBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AskAMuslimBackend.Infrastructure.Data.Configurations;

public class QuizAttemptConfiguration : IEntityTypeConfiguration<QuizAttempt>
{
    public void Configure(EntityTypeBuilder<QuizAttempt> builder)
    {
        builder.ToTable("QuizAttempts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Score)
            .HasPrecision(5, 2);

        builder.HasOne(a => a.Quiz)
            .WithMany()
            .HasForeignKey(a => a.QuizId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Student)
            .WithMany(s => s.QuizAttempts)
            .HasForeignKey(a => a.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.StudentId, a.QuizId });
        builder.HasIndex(a => new { a.StudentId, a.QuizId, a.AttemptNumber }).IsUnique();
    }
}
