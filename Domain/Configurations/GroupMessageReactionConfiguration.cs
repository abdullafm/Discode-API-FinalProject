using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class GroupMessageReactionConfiguration : IEntityTypeConfiguration<GroupMessageReaction>
    {
        public void Configure(EntityTypeBuilder<GroupMessageReaction> builder)
        {
            builder.Property(x => x.UserId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(x => x.Emoji)
                .IsRequired()
                .HasMaxLength(32);

            builder.HasOne(x => x.GroupMessage)
                .WithMany(m => m.Reactions)
                .HasForeignKey(x => x.GroupMessageId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.GroupMessageId, x.UserId, x.Emoji })
                .IsUnique();
        }
    }
}
