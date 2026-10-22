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
}
