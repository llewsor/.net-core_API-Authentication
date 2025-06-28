using AuthApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthApi.Data.Configurations
{
    public class UserConfiguration: IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(u => u.Id);
            builder.Property(u => u.Username)
                .IsRequired()
                .HasMaxLength(50);
            builder.Property(u => u.PasswordHash)
                .IsRequired();
            builder.Property(u => u.PasswordSalt)
                .IsRequired();
            builder.Property(u => u.IsBlocked)
                .HasDefaultValue(false);

            builder.HasIndex(u => u.Username)
                .IsUnique();

            builder.HasMany(u => u.RefreshTokens)
                   .WithOne(rt => rt.User)
                   .HasForeignKey(rt => rt.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
