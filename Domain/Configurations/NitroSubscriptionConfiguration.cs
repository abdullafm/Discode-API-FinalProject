using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class NitroSubscriptionConfiguration : IEntityTypeConfiguration<NitroSubscription>
    {
        public void Configure(EntityTypeBuilder<NitroSubscription> builder)
        {
            builder.Property(x => x.UserId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.StripeCustomerId).IsRequired().HasMaxLength(100).HasDefaultValue(string.Empty);
            builder.Property(x => x.StripeSubscriptionId).IsRequired().HasMaxLength(100).HasDefaultValue(string.Empty);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.UserId).IsUnique();
            builder.HasIndex(x => x.StripeSubscriptionId);
            builder.HasIndex(x => x.CurrentPeriodEnd);
        }
    }
}
