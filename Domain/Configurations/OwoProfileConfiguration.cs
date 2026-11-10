using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class OwoProfileConfiguration : IEntityTypeConfiguration<OwoProfile>
    {
        public void Configure(EntityTypeBuilder<OwoProfile> builder)
        {
            builder.Property(x => x.UserId).IsRequired().HasMaxLength(450);
            builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(x => x.UserId).IsUnique();
            builder.HasIndex(x => x.Cowoncy);
        }
    }

    public class OwoAnimalConfiguration : IEntityTypeConfiguration<OwoAnimal>
    {
        public void Configure(EntityTypeBuilder<OwoAnimal> builder)
        {
            builder.Property(x => x.UserId).IsRequired().HasMaxLength(450);
            builder.Property(x => x.AnimalKey).IsRequired().HasMaxLength(40);
            builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(x => new { x.UserId, x.AnimalKey }).IsUnique();
        }
    }
}
