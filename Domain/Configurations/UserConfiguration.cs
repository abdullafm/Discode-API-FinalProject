using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<AppUser>
    {
        public void Configure(EntityTypeBuilder<AppUser> builder)
        {
            builder.Property(u => u.DisplayName)
                .IsRequired()
                .HasMaxLength(32);

            builder.Property(u => u.BirthDay)
                .IsRequired();
        }
    }
}
