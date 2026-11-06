using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class ServerOverwriteConfiguration : IEntityTypeConfiguration<ServerOverwrite>
    {
        public void Configure(EntityTypeBuilder<ServerOverwrite> builder)
        {
            builder.ToTable("ServerOverwrites");

            builder.Property(x => x.UserId)
                .HasMaxLength(450);

            builder.HasOne(x => x.Channel)
                .WithMany()
                .HasForeignKey(x => x.ChannelId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.ServerId);
            builder.HasIndex(x => x.ChannelId);
            builder.HasIndex(x => x.CategoryId);
            builder.HasIndex(x => x.RoleId);
        }
    }
}
