using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureVideoStreaming.Models.Entities;

namespace SecureVideoStreaming.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(u => u.Id);

            builder.Property(u => u.Username)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(255);

            builder.HasIndex(u => u.Email)
                .IsUnique();

            builder.Property(u => u.UserType)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(u => u.PublicKeyRsa)
                .IsRequired();

            builder.Property(u => u.PasswordHash)
                .IsRequired();

            builder.Property(u => u.Salt)
                .IsRequired();

            builder.HasMany(u => u.OwnedVideos)
                .WithOne(v => v.Owner)
                .HasForeignKey(v => v.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(u => u.Permissions)
                .WithOne(p => p.Consumer)
                .HasForeignKey(p => p.ConsumerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}