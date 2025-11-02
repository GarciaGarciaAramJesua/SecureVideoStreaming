using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureVideoStreaming.Models.Entities;

namespace SecureVideoStreaming.Data.Configurations
{
    public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
    {
        public void Configure(EntityTypeBuilder<Permission> builder)
        {
            builder.HasKey(p => p.Id);

            builder.HasIndex(p => new { p.VideoId, p.ConsumerId })
                .IsUnique();

            builder.Property(p => p.IsRevoked)
                .IsRequired()
                .HasDefaultValue(false);
        }
    }
}