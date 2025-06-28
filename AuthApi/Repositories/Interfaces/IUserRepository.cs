using AuthApi.Models;

namespace AuthApi.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task AddAsync(User user);
        Task<User?> GetByUsernameAsync(string username);

        Task SaveChangesAsync();
    }
}
