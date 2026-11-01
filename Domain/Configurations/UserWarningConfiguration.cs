using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class UserWarningConfiguration : IEntityTypeConfiguration<UserWarning>
    {
        public void Configure(EntityTypeBuilder<UserWarning> builder)
        {
            builder.Property(x => x.UserId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.IssuedById).IsRequired().HasMaxLength(450);
            builder.Property(x => x.Reason).IsRequired().HasMaxLength(300);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.UserId, x.CreatedAt });
        }
    }
}
