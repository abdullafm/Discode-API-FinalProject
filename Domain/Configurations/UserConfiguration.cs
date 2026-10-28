using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<AppUser>
    {
        public void Configure(EntityTypeBuilder<AppUser> builder)
        {
            builder.Property(u => u.PublicId).IsRequired().HasMaxLength(20);
            builder.HasIndex(u => u.PublicId).IsUnique();

            builder.Property(u => u.DisplayName)
                .IsRequired()
                .HasMaxLength(32);

            builder.Property(u => u.BirthDay)
                .IsRequired();

            builder.Property(u => u.AvatarUrl)
                .HasMaxLength(500);

            builder.Property(u => u.Bio)
                .HasMaxLength(190);

            builder.Property(u => u.Pronouns)
                .HasMaxLength(40);

            builder.Property(u => u.CustomStatus)
                .HasMaxLength(128);

            builder.Property(u => u.IsPublic)
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(u => u.PreferredStatus)
                .IsRequired()
                .HasDefaultValue(PresenceStatus.Online);

            builder.Property(u => u.EmailVerificationCode)
                .HasMaxLength(4);
        }
    }
}
