using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class ServerVoiceStateConfiguration : IEntityTypeConfiguration<ServerVoiceState>
    {
        public void Configure(EntityTypeBuilder<ServerVoiceState> builder)
        {
            builder.ToTable("ServerVoiceStates");

            builder.Property(x => x.UserId)
                .IsRequired()
                .HasMaxLength(450);

            builder.HasOne(x => x.Channel)
                .WithMany()
                .HasForeignKey(x => x.ChannelId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.UserId)
                .IsUnique();
            builder.HasIndex(x => x.ServerId);
            builder.HasIndex(x => x.ChannelId);
        }
    }
}
