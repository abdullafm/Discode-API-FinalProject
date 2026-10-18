using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class StoryConfiguration : IEntityTypeConfiguration<Story>
    {
        public void Configure(EntityTypeBuilder<Story> builder)
        {
            builder.Property(x => x.AuthorId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(x => x.ImageUrl)
                .IsRequired()
                .HasMaxLength(200);

            builder.HasOne(x => x.Author)
                .WithMany()
                .HasForeignKey(x => x.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.CreatedAt);
        }
    }
}
