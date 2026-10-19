using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class EmojiConfiguration : IEntityTypeConfiguration<Emoji>
    {
        public void Configure(EntityTypeBuilder<Emoji> builder)
        {
            builder.Property(x => x.Character)
                .IsRequired()
                .HasMaxLength(32);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Shortcodes)
                .IsRequired()
                .HasMaxLength(200);

            builder.HasOne(x => x.EmojiCategory)
                .WithMany(c => c.Emojis)
                .HasForeignKey(x => x.EmojiCategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.EmojiCategoryId, x.SortOrder });
        }
    }
}
