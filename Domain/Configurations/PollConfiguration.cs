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

    public class PollOptionConfiguration : IEntityTypeConfiguration<PollOption>
    {
        public void Configure(EntityTypeBuilder<PollOption> builder)
        {
            builder.Property(x => x.Text)
                .IsRequired()
                .HasMaxLength(55);

            builder.Property(x => x.Emoji)
                .IsRequired()
                .HasMaxLength(32);

            builder.HasOne(x => x.Poll)
                .WithMany(p => p.Options)
                .HasForeignKey(x => x.PollId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public class PollVoteConfiguration : IEntityTypeConfiguration<PollVote>
    {
        public void Configure(EntityTypeBuilder<PollVote> builder)
        {
            builder.Property(x => x.UserId)
                .IsRequired()
                .HasMaxLength(450);

            builder.HasOne(x => x.Option)
                .WithMany(o => o.Votes)
                .HasForeignKey(x => x.OptionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.OptionId, x.UserId })
                .IsUnique();
        }
    }
}
