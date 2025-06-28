using AuthApi.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Data
{
    public interface IDataContext
    {
        DbSet<User> Users { get; }
        DbSet<RefreshToken> RefreshTokens { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
