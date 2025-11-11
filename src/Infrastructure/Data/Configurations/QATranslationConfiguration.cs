using AskAMuslimBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AskAMuslimBackend.Infrastructure.Data.Configurations;

public class QATranslationConfiguration : IEntityTypeConfiguration<QATranslation>
{
    public void Configure(EntityTypeBuilder<QATranslation> builder)
    {
        builder.ToTable("QATranslations");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Language)
            .IsRequired()
            .HasConversion<string>();
        builder.Property(t => t.QuestionText).IsRequired();
        builder.Property(t => t.AnswerText).IsRequired();

        builder.HasIndex(t => new { t.QAId, t.Language }).IsUnique();
    }
}
