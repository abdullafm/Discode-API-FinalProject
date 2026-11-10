using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class NitroEventConfiguration : IEntityTypeConfiguration<NitroEvent>
    {
        public void Configure(EntityTypeBuilder<NitroEvent> builder)
        {
            builder.Property(x => x.UserId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.Details).IsRequired().HasMaxLength(500);
            builder.Property(x => x.Currency).IsRequired().HasMaxLength(3).HasDefaultValue(string.Empty);
            builder.Property(x => x.StripeEventId).HasMaxLength(100);
            builder.Property(x => x.ActorId).HasMaxLength(450);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.StripeEventId).IsUnique();
            builder.HasIndex(x => x.CreatedAt);
        }
    }
}
