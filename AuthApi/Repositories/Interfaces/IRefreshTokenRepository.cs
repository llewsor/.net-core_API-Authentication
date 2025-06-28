using AuthApi.Models;

namespace AuthApi.Repositories.Interfaces
{
    public interface IRefreshTokenRepository
    {
        Task AddAsync(RefreshToken refreshToken);
        Task<RefreshToken?> GetByTokenAsync(string token);

        Task SaveChangesAsync();
    }
}
