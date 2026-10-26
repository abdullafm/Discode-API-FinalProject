using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class ServerChannelAccessConfiguration : IEntityTypeConfiguration<ServerChannelAccess>
    {
        public void Configure(EntityTypeBuilder<ServerChannelAccess> builder)
        {
            builder.Property(x => x.UserId)
                .HasMaxLength(450);

            builder.HasOne(x => x.Channel)
                .WithMany()
                .HasForeignKey(x => x.ChannelId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.ChannelId);
            builder.HasIndex(x => x.RoleId);
            builder.HasIndex(x => x.UserId);
        }
    }
}
