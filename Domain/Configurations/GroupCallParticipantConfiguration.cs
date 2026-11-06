using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class GroupCallParticipantConfiguration : IEntityTypeConfiguration<GroupCallParticipant>
    {
        public void Configure(EntityTypeBuilder<GroupCallParticipant> builder)
        {
            builder.Property(x => x.UserId)
                .IsRequired()
                .HasMaxLength(450);

            builder.HasOne(x => x.GroupCall)
                .WithMany(c => c.Participants)
                .HasForeignKey(x => x.GroupCallId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.GroupCallId, x.UserId })
                .IsUnique();
        }
    }
}
