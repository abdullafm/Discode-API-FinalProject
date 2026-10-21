using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class MessageForwardConfiguration : IEntityTypeConfiguration<MessageForward>
    {
        public void Configure(EntityTypeBuilder<MessageForward> builder)
        {
            builder.Property(x => x.OriginalSenderId)
                .IsRequired()
                .HasMaxLength(450);

            builder.HasOne(x => x.Message)
                .WithOne(m => m.Forward)
                .HasForeignKey<MessageForward>(x => x.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.OriginalSender)
                .WithMany()
                .HasForeignKey(x => x.OriginalSenderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.MessageId)
                .IsUnique();
        }
    }
}
