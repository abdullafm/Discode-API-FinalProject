using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class ServerConfiguration : IEntityTypeConfiguration<Server>
    {
        public void Configure(EntityTypeBuilder<Server> builder)
        {
            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.IconUrl)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.InviteCode)
                .IsRequired()
                .HasMaxLength(16);

            builder.Property(x => x.Description)
                .IsRequired()
                .HasMaxLength(300);

            builder.Property(x => x.BannerColor)
                .IsRequired()
                .HasMaxLength(16)
                .HasDefaultValue("ink");

            builder.Property(x => x.OwnerId)
                .IsRequired()
                .HasMaxLength(450);

            builder.HasOne(x => x.Owner)
                .WithMany()
                .HasForeignKey(x => x.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.InviteCode)
                .IsUnique();
        }
    }
}
