using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class ServerMessageAttachmentConfiguration : IEntityTypeConfiguration<ServerMessageAttachment>
    {
        public void Configure(EntityTypeBuilder<ServerMessageAttachment> builder)
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

            builder.HasOne(x => x.ServerMessage)
                .WithMany(m => m.Attachments)
                .HasForeignKey(x => x.ServerMessageId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
