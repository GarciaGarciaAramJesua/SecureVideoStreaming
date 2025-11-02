using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureVideoStreaming.Models.Entities;

namespace SecureVideoStreaming.Data.Configurations
{
    public class VideoConfiguration : IEntityTypeConfiguration<Video>
    {
        public void Configure(EntityTypeBuilder<Video> builder)
        {
            builder.HasKey(v => v.Id);

            builder.Property(v => v.Title)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(v => v.Description)
                .HasMaxLength(1000);

            builder.Property(v => v.EncryptedFilePath)
                .IsRequired();

            builder.Property(v => v.EncryptedKek)
                .IsRequired();

            builder.Property(v => v.Nonce)
                .IsRequired()
                .HasMaxLength(32); // Longitud en Base64 para 12 bytes en Base64 = ~16 chars, ponemos 32 por seguridad

            builder.Property(v => v.AuthTag)
                .IsRequired()
                .HasMaxLength(64); // Longitud en Base64 para 16 bytes en Base64 = ~24 chars, ponemos 64 por seguridad

            builder.Property(v => v.Hmac)
                .IsRequired();

            builder.Property(v => v.OriginalHash)
                .IsRequired()
                .HasMaxLength(64); // SHA-256 en hex

            builder.HasMany(v => v.Permissions)
                .WithOne(p => p.Video)
                .HasForeignKey(p => p.VideoId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}