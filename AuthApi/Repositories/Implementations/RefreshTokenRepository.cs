using AuthApi.Data;
using AuthApi.Models;
using AuthApi.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Repositories.Implementations
{
    public class RefreshTokenRepository: IRefreshTokenRepository
    {
        private readonly IDataContext _context;

        public RefreshTokenRepository(IDataContext context)
        {
            _context = context;
        }

        public async Task AddAsync(RefreshToken token) =>
            await _context.RefreshTokens.AddAsync(token);
        
        public Task<RefreshToken?> GetByTokenAsync(string token) =>
            _context.RefreshTokens.Include(x=>x.User).SingleOrDefaultAsync(rt => rt.Token == token);

        public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
    }
}
