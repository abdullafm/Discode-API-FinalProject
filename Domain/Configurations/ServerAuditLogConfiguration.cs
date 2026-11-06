using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class ServerAuditLogConfiguration : IEntityTypeConfiguration<ServerAuditLog>
    {
        public void Configure(EntityTypeBuilder<ServerAuditLog> builder)
        {
            builder.Property(x => x.ActorId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(x => x.TargetType)
                .IsRequired()
                .HasMaxLength(32);

            builder.Property(x => x.TargetId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(x => x.TargetName)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.Details)
                .IsRequired()
                .HasMaxLength(1000);

            builder.HasOne(x => x.Server)
                .WithMany()
                .HasForeignKey(x => x.ServerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Actor)
                .WithMany()
                .HasForeignKey(x => x.ActorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.ServerId, x.Id });
        }
    }
}
