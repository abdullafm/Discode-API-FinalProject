using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class CommentConfiguration : IEntityTypeConfiguration<Comment>
    {
        public void Configure(EntityTypeBuilder<Comment> builder)
        {
            builder.Property(x => x.AuthorId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(x => x.Text)
                .IsRequired()
                .HasMaxLength(280);

            builder.HasOne(x => x.Post)
                .WithMany(p => p.Comments)
                .HasForeignKey(x => x.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Author)
                .WithMany()
                .HasForeignKey(x => x.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.ParentComment)
                .WithMany(c => c.Replies)
                .HasForeignKey(x => x.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.PostId);
            builder.HasIndex(x => x.ParentCommentId);
        }
    }
}
