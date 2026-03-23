using AskAMuslimBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AskAMuslimBackend.Infrastructure.Data.Configurations;

public class LearnIslamRequestConfiguration : IEntityTypeConfiguration<LearnIslamRequest>
{
    public void Configure(EntityTypeBuilder<LearnIslamRequest> builder)
    {
        builder.Property(t => t.FullName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.Email)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.Country)
            .HasMaxLength(100)
            .IsRequired();
            
        builder.Property(t => t.Message)
            .HasMaxLength(2000);
            
        builder.Property(t => t.CurrentReligion)
            .HasMaxLength(100);
            
        builder.Property(t => t.Label)
            .HasMaxLength(100);
    }
}