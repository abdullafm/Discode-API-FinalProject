using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class ServerBanConfiguration : IEntityTypeConfiguration<ServerBan>
    {
        public void Configure(EntityTypeBuilder<ServerBan> builder)
        {
            builder.Property(x => x.UserId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(x => x.BannedById)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(x => x.Reason)
                .IsRequired()
                .HasMaxLength(300);

            builder.HasOne(x => x.Server)
                .WithMany()
                .HasForeignKey(x => x.ServerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.BannedBy)
                .WithMany()
                .HasForeignKey(x => x.BannedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.ServerId, x.UserId })
                .IsUnique();

            builder.HasIndex(x => new { x.ServerId, x.Id });
        }
    }
}
