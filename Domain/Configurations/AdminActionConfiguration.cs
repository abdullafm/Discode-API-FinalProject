using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class AdminActionConfiguration : IEntityTypeConfiguration<AdminAction>
    {
        public void Configure(EntityTypeBuilder<AdminAction> builder)
        {
            builder.Property(x => x.AdminId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.TargetId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.TargetName).IsRequired().HasMaxLength(200);
            builder.Property(x => x.Reason).IsRequired().HasMaxLength(300);
            builder.Property(x => x.Details).IsRequired().HasMaxLength(300);

            builder.HasOne(x => x.Admin)
                .WithMany()
                .HasForeignKey(x => x.AdminId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.TargetType, x.TargetId, x.CreatedAt });
            builder.HasIndex(x => x.CreatedAt);
        }
    }
}
