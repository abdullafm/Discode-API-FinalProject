using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class PollConfiguration : IEntityTypeConfiguration<Poll>
    {
        public void Configure(EntityTypeBuilder<Poll> builder)
        {
            builder.Property(x => x.Question)
                .IsRequired()
                .HasMaxLength(300);

            builder.HasOne(x => x.Message)
                .WithOne(m => m.Poll)
                .HasForeignKey<Poll>(x => x.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.GroupMessage)
                .WithOne(m => m.Poll)
                .HasForeignKey<Poll>(x => x.GroupMessageId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.MessageId)
                .IsUnique();

            builder.HasIndex(x => x.GroupMessageId)
                .IsUnique();
        }
    }
}
