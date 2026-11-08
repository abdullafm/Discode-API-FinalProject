using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class BoostCooldownConfiguration : IEntityTypeConfiguration<BoostCooldown>
    {
        public void Configure(EntityTypeBuilder<BoostCooldown> builder)
        {
            builder.Property(x => x.UserId).IsRequired().HasMaxLength(450);

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.UserId, x.AvailableAt });
        }
    }
}
