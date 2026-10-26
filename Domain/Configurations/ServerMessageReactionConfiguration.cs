using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class ServerMessageReactionConfiguration : IEntityTypeConfiguration<ServerMessageReaction>
    {
        public void Configure(EntityTypeBuilder<ServerMessageReaction> builder)
        {
            builder.Property(x => x.UserId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(x => x.Emoji)
                .IsRequired()
                .HasMaxLength(32);

            builder.HasOne(x => x.ServerMessage)
                .WithMany(m => m.Reactions)
                .HasForeignKey(x => x.ServerMessageId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.ServerMessageId, x.UserId, x.Emoji })
                .IsUnique();
        }
    }
}
