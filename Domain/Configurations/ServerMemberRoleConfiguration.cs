using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class ServerMemberRoleConfiguration : IEntityTypeConfiguration<ServerMemberRole>
    {
        public void Configure(EntityTypeBuilder<ServerMemberRole> builder)
        {
            builder.HasOne(x => x.ServerMember)
                .WithMany()
                .HasForeignKey(x => x.ServerMemberId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Role)
                .WithMany(r => r.MemberRoles)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.ServerMemberId, x.RoleId })
                .IsUnique();
        }
    }
}
