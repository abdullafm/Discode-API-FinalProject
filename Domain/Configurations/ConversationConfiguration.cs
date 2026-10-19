using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
    {
        public void Configure(EntityTypeBuilder<Conversation> builder)
        {
            builder.Property(x => x.UserAId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(x => x.UserBId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(x => x.LastSenderId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(x => x.LastMessageText)
                .IsRequired()
                .HasMaxLength(200);

            builder.HasOne(x => x.UserA)
                .WithMany()
                .HasForeignKey(x => x.UserAId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.UserB)
                .WithMany()
                .HasForeignKey(x => x.UserBId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.UserAId, x.UserBId })
                .IsUnique();
        }
    }
}
