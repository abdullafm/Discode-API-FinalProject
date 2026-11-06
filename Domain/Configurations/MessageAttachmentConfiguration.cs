using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class MessageAttachmentConfiguration : IEntityTypeConfiguration<MessageAttachment>
    {
        public void Configure(EntityTypeBuilder<MessageAttachment> builder)
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

            builder.HasOne(x => x.Message)
                .WithMany(m => m.Attachments)
                .HasForeignKey(x => x.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.MessageId);
        }
    }
}
