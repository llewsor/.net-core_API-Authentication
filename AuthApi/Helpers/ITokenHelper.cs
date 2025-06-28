using AuthApi.Models;

namespace AuthApi.Helpers
{
    public interface ITokenHelper
    {
        string GenerateToken(User user);
        RefreshToken CreateRefreshToken(int userId, string ip);
    }
}
