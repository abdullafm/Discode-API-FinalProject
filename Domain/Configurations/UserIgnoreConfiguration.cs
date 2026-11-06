using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class UserIgnoreConfiguration : IEntityTypeConfiguration<UserIgnore>
    {
        public void Configure(EntityTypeBuilder<UserIgnore> builder)
        {
            builder.Property(x => x.IgnorerId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(x => x.IgnoredId)
                .IsRequired()
                .HasMaxLength(450);

            builder.HasOne(x => x.Ignorer)
                .WithMany()
                .HasForeignKey(x => x.IgnorerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Ignored)
                .WithMany()
                .HasForeignKey(x => x.IgnoredId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => new { x.IgnorerId, x.IgnoredId })
                .IsUnique();
        }
    }
}
