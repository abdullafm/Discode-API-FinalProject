using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class LiveModeratorConfiguration : IEntityTypeConfiguration<LiveModerator>
    {
        public void Configure(EntityTypeBuilder<LiveModerator> builder)
        {
            builder.Property(x => x.HostId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.UserId).IsRequired().HasMaxLength(450);

            builder.HasOne(x => x.Host).WithMany().HasForeignKey(x => x.HostId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.HostId, x.UserId }).IsUnique();
        }
    }

    public class LiveChatMuteConfiguration : IEntityTypeConfiguration<LiveChatMute>
    {
        public void Configure(EntityTypeBuilder<LiveChatMute> builder)
        {
            builder.Property(x => x.HostId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.UserId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.MutedById).IsRequired().HasMaxLength(450);

            builder.HasOne(x => x.Host).WithMany().HasForeignKey(x => x.HostId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.HostId, x.UserId }).IsUnique();
        }
    }
}
