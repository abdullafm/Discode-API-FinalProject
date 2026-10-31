using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class TranslationConfiguration : IEntityTypeConfiguration<Translation>
    {
        public void Configure(EntityTypeBuilder<Translation> builder)
        {
            builder.Property(x => x.LanguageCode)
                .IsRequired()
                .HasMaxLength(8);

            builder.Property(x => x.Key)
                .IsRequired()
                .HasMaxLength(300)
                .UseCollation("Latin1_General_100_CS_AS");

            builder.Property(x => x.Value)
                .IsRequired()
                .HasMaxLength(600);

            builder.HasIndex(x => new { x.LanguageCode, x.Key })
                .IsUnique();
        }
    }
}
