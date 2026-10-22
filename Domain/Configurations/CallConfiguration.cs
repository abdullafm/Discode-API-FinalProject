using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Configurations
{
    public class CallConfiguration : IEntityTypeConfiguration<Call>
    {
        public void Configure(EntityTypeBuilder<Call> builder)
        {
            builder.Property(x => x.CallerId)
                .IsRequired()
                .HasMaxLength(450);

            builder.Property(x => x.CalleeId)
                .IsRequired()
                .HasMaxLength(450);

            builder.HasOne(x => x.Caller)
                .WithMany()
                .HasForeignKey(x => x.CallerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Callee)
                .WithMany()
                .HasForeignKey(x => x.CalleeId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.EndedAt);
            builder.HasIndex(x => new { x.CallerId, x.CalleeId });
        }
    }
}
