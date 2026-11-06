using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class ServerMessageConfiguration : IEntityTypeConfiguration<ServerMessage>
    {
        public void Configure(EntityTypeBuilder<ServerMessage> builder)
        {
            builder.Property(x => x.PublicId).IsRequired().HasMaxLength(20);
            builder.HasIndex(x => x.PublicId).IsUnique();

            builder.Property(x => x.SenderId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(x => x.Text)
                .IsRequired()
                .HasMaxLength(2000);

            builder.HasOne(x => x.Server)
                .WithMany()
                .HasForeignKey(x => x.ServerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Channel)
                .WithMany()
                .HasForeignKey(x => x.ChannelId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Sender)
                .WithMany()
                .HasForeignKey(x => x.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.ReplyTo)
                .WithMany()
                .HasForeignKey(x => x.ReplyToMessageId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.ChannelId, x.Id });
        }
    }
}
