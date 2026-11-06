using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class ServerCategoryConfiguration : IEntityTypeConfiguration<ServerCategory>
    {
        public void Configure(EntityTypeBuilder<ServerCategory> builder)
        {
            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasOne(x => x.Server)
                .WithMany()
                .HasForeignKey(x => x.ServerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => new { x.ServerId, x.Position });
        }
    }
}
