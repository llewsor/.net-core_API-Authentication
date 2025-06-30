using AuthApi.Models;

namespace AuthApi.Helpers
{
    public interface ITokenHelper
    {
        string CreateToken(User user);
        RefreshToken CreateRefreshToken(int userId, string ip);
    }
}
