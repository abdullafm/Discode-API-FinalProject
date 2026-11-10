using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class ServerBoostConfiguration : IEntityTypeConfiguration<ServerBoost>
    {
        public void Configure(EntityTypeBuilder<ServerBoost> builder)
        {
            builder.Property(x => x.UserId).IsRequired().HasMaxLength(450);

            builder.HasOne(x => x.Server)
                .WithMany()
                .HasForeignKey(x => x.ServerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.ServerId);
            builder.HasIndex(x => x.UserId);
        }
    }
}
