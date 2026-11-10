using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class ServerTemplateConfiguration : IEntityTypeConfiguration<ServerTemplate>
    {
        public void Configure(EntityTypeBuilder<ServerTemplate> builder)
        {
            builder.Property(x => x.Code).IsRequired().HasMaxLength(20);
            builder.Property(x => x.Title).IsRequired().HasMaxLength(100);
            builder.Property(x => x.Description).HasMaxLength(120);
            builder.Property(x => x.CreatedById).IsRequired().HasMaxLength(450);
            builder.Property(x => x.Snapshot).IsRequired();

            builder.HasOne(x => x.Server).WithMany().HasForeignKey(x => x.ServerId).OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(x => x.Code).IsUnique();
            builder.HasIndex(x => x.ServerId).IsUnique();
        }
    }
}
