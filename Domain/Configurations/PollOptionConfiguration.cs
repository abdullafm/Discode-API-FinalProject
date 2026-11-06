using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
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
}
