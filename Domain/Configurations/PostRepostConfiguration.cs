using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class PostRepostConfiguration : IEntityTypeConfiguration<PostRepost>
    {
        public void Configure(EntityTypeBuilder<PostRepost> builder)
        {
            builder.Property(x => x.UserId)
                .IsRequired()
                .HasMaxLength(450);

            builder.HasOne(x => x.Post)
                .WithMany(p => p.Reposts)
                .HasForeignKey(x => x.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.PostId, x.UserId })
                .IsUnique();
        }
    }
}
