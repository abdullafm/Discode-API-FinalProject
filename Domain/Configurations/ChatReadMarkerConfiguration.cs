using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class ChatReadMarkerConfiguration : IEntityTypeConfiguration<ChatReadMarker>
    {
        public void Configure(EntityTypeBuilder<ChatReadMarker> builder)
        {
            builder.Property(x => x.UserId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.Kind).IsRequired().HasMaxLength(10);
            builder.Property(x => x.Key).IsRequired().HasMaxLength(100);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.UserId, x.Kind, x.Key }).IsUnique();
        }
    }
}
