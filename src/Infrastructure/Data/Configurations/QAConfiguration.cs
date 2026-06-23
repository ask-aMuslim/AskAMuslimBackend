using AskAMuslimBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AskAMuslimBackend.Infrastructure.Data.Configurations;

public class QAConfiguration : IEntityTypeConfiguration<QA>
{
    public void Configure(EntityTypeBuilder<QA> builder)
    {
        builder.ToTable("QAs");
        builder.HasKey(q => q.Id);

        builder.Property(q => q.Order)
            .HasDefaultValue(0);

        builder.HasMany(q => q.Translations)
            .WithOne(t => t.QA)
            .HasForeignKey(t => t.QAId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
