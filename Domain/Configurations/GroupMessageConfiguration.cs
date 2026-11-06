using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class GroupMessageConfiguration : IEntityTypeConfiguration<GroupMessage>
    {
        public void Configure(EntityTypeBuilder<GroupMessage> builder)
        {
            builder.Property(x => x.PublicId).IsRequired().HasMaxLength(20);
            builder.HasIndex(x => x.PublicId).IsUnique();

            builder.Property(x => x.Text)
                .IsRequired()
                .HasMaxLength(2000);

            builder.Property(x => x.SenderId)
                .IsRequired()
                .HasMaxLength(450);

            builder.HasOne(x => x.Group)
                .WithMany(g => g.Messages)
                .HasForeignKey(x => x.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Sender)
                .WithMany()
                .HasForeignKey(x => x.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.ReplyTo)
                .WithMany()
                .HasForeignKey(x => x.ReplyToMessageId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasIndex(x => new { x.GroupId, x.CreatedAt });
        }
    }
}
