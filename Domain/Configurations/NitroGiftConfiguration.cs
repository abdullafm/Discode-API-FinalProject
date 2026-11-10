using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class NitroGiftConfiguration : IEntityTypeConfiguration<NitroGift>
    {
        public void Configure(EntityTypeBuilder<NitroGift> builder)
        {
            builder.Property(x => x.Code).IsRequired().HasMaxLength(40);
            builder.Property(x => x.BuyerId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.RedeemedById).HasMaxLength(450);
            builder.Property(x => x.StripeSessionId).HasMaxLength(200);

            builder.HasOne(x => x.Buyer).WithMany().HasForeignKey(x => x.BuyerId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(x => x.RedeemedBy).WithMany().HasForeignKey(x => x.RedeemedById).OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.Code).IsUnique();
            builder.HasIndex(x => x.StripeSessionId).IsUnique().HasFilter("[StripeSessionId] IS NOT NULL");
        }
    }
}
