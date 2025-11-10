using AskAMuslimBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AskAMuslimBackend.Infrastructure.Data.Configurations;

public class QATagConfiguration : IEntityTypeConfiguration<QATag>
{
    public void Configure(EntityTypeBuilder<QATag> builder)
    {
        builder.ToTable("QATags");
        builder.HasKey(ct => new { ct.QAId, ct.TagId });

        builder.HasOne(ct => ct.QA)
            .WithMany(q => q.QATags)
            .HasForeignKey(ct => ct.QAId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ct => ct.Tag)
            .WithMany(t => t.QATags)
            .HasForeignKey(ct => ct.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
