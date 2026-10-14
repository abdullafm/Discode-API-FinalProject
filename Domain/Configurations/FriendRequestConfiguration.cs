using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class FriendRequestConfiguration : IEntityTypeConfiguration<FriendRequest>
    {
        public void Configure(EntityTypeBuilder<FriendRequest> builder)
        {
            builder.Property(x => x.SenderId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(x => x.ReceiverId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(x => x.Status)
                .IsRequired();

            builder.HasOne(x => x.Sender)
                .WithMany(u => u.SentRequests)
                .HasForeignKey(x => x.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Receiver)
                .WithMany(u => u.ReceivedRequests)
                .HasForeignKey(x => x.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.SenderId, x.ReceiverId })
                .IsUnique();
        }
    }
}
