using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class GroupCallConfiguration : IEntityTypeConfiguration<GroupCall>
    {
        public void Configure(EntityTypeBuilder<GroupCall> builder)
        {
            builder.Property(x => x.StarterId)
                .IsRequired()
                .HasMaxLength(450);

            builder.HasOne(x => x.Group)
                .WithMany()
                .HasForeignKey(x => x.GroupId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(x => x.Starter)
                .WithMany()
                .HasForeignKey(x => x.StarterId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.GroupMessage)
                .WithOne(m => m.Call)
                .HasForeignKey<GroupCall>(x => x.GroupMessageId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.GroupId, x.Active });
        }
    }
}
