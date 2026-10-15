using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class UserBlockConfiguration : IEntityTypeConfiguration<UserBlock>
    {
        public void Configure(EntityTypeBuilder<UserBlock> builder)
        {
            builder.Property(x => x.BlockerId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(x => x.BlockedId)
                .IsRequired()
                .HasMaxLength(450);

            builder.HasOne(x => x.Blocker)
                .WithMany(u => u.BlockedUsers)
                .HasForeignKey(x => x.BlockerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Blocked)
                .WithMany(u => u.BlockedByUsers)
                .HasForeignKey(x => x.BlockedId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.BlockerId, x.BlockedId })
                .IsUnique();
        }
    }
}
