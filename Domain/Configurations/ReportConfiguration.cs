using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class ReportConfiguration : IEntityTypeConfiguration<Report>
    {
        public void Configure(EntityTypeBuilder<Report> builder)
        {
            builder.Property(x => x.ReporterId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(x => x.TargetId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(x => x.TargetOwnerId)
                .HasMaxLength(450);

            builder.Property(x => x.Details)
                .IsRequired()
                .HasMaxLength(500);

            builder.Property(x => x.ContentSnapshot)
                .IsRequired()
                .HasMaxLength(2000);

            builder.HasOne(x => x.Reporter)
                .WithMany()
                .HasForeignKey(x => x.ReporterId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.ReporterId, x.TargetType, x.TargetId });
            builder.HasIndex(x => new { x.Status, x.CreatedAt });
        }
    }
}
