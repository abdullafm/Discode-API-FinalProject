using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
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
}
