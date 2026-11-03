using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class ServerVoiceRestrictionConfiguration : IEntityTypeConfiguration<ServerVoiceRestriction>
    {
        public void Configure(EntityTypeBuilder<ServerVoiceRestriction> builder)
        {
            builder.ToTable("ServerVoiceRestrictions");

            builder.Property(x => x.UserId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.ByUserId).IsRequired().HasMaxLength(450);

            builder.HasOne(x => x.Server)
                .WithMany()
                .HasForeignKey(x => x.ServerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.ServerId, x.UserId }).IsUnique();
        }
    }
}
