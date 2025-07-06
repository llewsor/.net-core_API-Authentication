using AuthApi.Data;
using AuthApi.Models;
using AuthApi.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Repositories.Implementations
{
    public class UserRepository(DataContext context) : IUserRepository
    {
        private readonly IDataContext _context = context;

        public async Task AddAsync(User user)=>
            await _context.Users.AddAsync(user);
        
        public async Task<User?> GetByUsernameAsync(string username) =>
            await _context.Users.SingleOrDefaultAsync(u => u.Username == username);

        public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
    }
}
