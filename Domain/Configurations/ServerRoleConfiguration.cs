using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class ServerRoleConfiguration : IEntityTypeConfiguration<ServerRole>
    {
        public void Configure(EntityTypeBuilder<ServerRole> builder)
        {
            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Color)
                .IsRequired()
                .HasMaxLength(16)
                .HasDefaultValue("gray");

            builder.HasOne(x => x.Server)
                .WithMany(s => s.Roles)
                .HasForeignKey(x => x.ServerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.ServerId, x.Name })
                .IsUnique();
        }
    }
}
