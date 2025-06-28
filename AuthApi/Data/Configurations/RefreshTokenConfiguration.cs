using AuthApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthApi.Data.Configurations
{
    public class RefreshTokenConfiguration: IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.HasKey(rt => rt.Id);
            builder.Property(rt => rt.Token)
                .IsRequired()
                .HasMaxLength(256);
            builder.Property(rt => rt.Expires)
                .IsRequired();
            builder.Property(rt => rt.Created)
                .IsRequired();

            // Unique constraint on Token
            builder.HasIndex(rt => rt.Token)
                .IsUnique();
            // Foreign key relationship with User
            builder.HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
