using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class GroupConfiguration : IEntityTypeConfiguration<Group>
    {
        public void Configure(EntityTypeBuilder<Group> builder)
        {
            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.IconUrl)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.OwnerId)
                .IsRequired()
                .HasMaxLength(450);

            builder.HasOne(x => x.Owner)
                .WithMany()
                .HasForeignKey(x => x.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }

    public class GroupMemberConfiguration : IEntityTypeConfiguration<GroupMember>
    {
        public void Configure(EntityTypeBuilder<GroupMember> builder)
        {
            builder.Property(x => x.UserId)
                .IsRequired()
                .HasMaxLength(450);

            builder.HasOne(x => x.Group)
                .WithMany(g => g.Members)
                .HasForeignKey(x => x.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.GroupId, x.UserId })
                .IsUnique();
        }
    }

    public class GroupMessageConfiguration : IEntityTypeConfiguration<GroupMessage>
    {
        public void Configure(EntityTypeBuilder<GroupMessage> builder)
        {
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

    public class GroupMessageAttachmentConfiguration : IEntityTypeConfiguration<GroupMessageAttachment>
    {
        public void Configure(EntityTypeBuilder<GroupMessageAttachment> builder)
        {
            builder.Property(x => x.Url)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.FileName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.ContentType)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasOne(x => x.GroupMessage)
                .WithMany(m => m.Attachments)
                .HasForeignKey(x => x.GroupMessageId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.GroupMessageId);
        }
    }

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
