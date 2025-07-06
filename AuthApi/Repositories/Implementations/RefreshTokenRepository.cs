using AuthApi.Data;
using AuthApi.Models;
using AuthApi.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Repositories.Implementations
{
    public class RefreshTokenRepository(IDataContext context) : IRefreshTokenRepository
    {
        public async Task AddAsync(RefreshToken token) =>
            await context.RefreshTokens.AddAsync(token);
        
        public Task<RefreshToken?> GetByTokenAsync(string token) =>
            context.RefreshTokens.Include(x=>x.User).SingleOrDefaultAsync(rt => rt.Token == token);

        public async Task SaveChangesAsync() => await context.SaveChangesAsync();
    }
}
