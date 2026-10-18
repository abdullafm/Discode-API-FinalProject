using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class LiveStreamConfiguration : IEntityTypeConfiguration<LiveStream>
    {
        public void Configure(EntityTypeBuilder<LiveStream> builder)
        {
            builder.Property(x => x.HostId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(70);

            builder.Property(x => x.Category)
                .IsRequired()
                .HasMaxLength(40);

            builder.Property(x => x.ThumbnailUrl)
                .HasMaxLength(100);

            builder.HasOne(x => x.Host)
                .WithMany()
                .HasForeignKey(x => x.HostId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.HostId, x.EndedAt });
        }
    }
}
