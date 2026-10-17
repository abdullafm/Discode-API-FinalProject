using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class PostConfiguration : IEntityTypeConfiguration<Post>
    {
        public void Configure(EntityTypeBuilder<Post> builder)
        {
            builder.Property(x => x.AuthorId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(x => x.Text)
                .HasMaxLength(500);

            builder.Property(x => x.ImageUrl)
                .HasMaxLength(200);

            builder.HasOne(x => x.Author)
                .WithMany(u => u.Posts)
                .HasForeignKey(x => x.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.CreatedAt);
        }
    }
}
