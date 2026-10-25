using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class ServerMemberConfiguration : IEntityTypeConfiguration<ServerMember>
    {
        public void Configure(EntityTypeBuilder<ServerMember> builder)
        {
            builder.Property(x => x.UserId)
                .IsRequired()
                .HasMaxLength(450);

            builder.HasOne(x => x.Server)
                .WithMany(s => s.Members)
                .HasForeignKey(x => x.ServerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.ServerId, x.UserId })
                .IsUnique();
        }
    }
}
