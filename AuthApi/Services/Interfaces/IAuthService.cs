using AuthApi.Models;
using Azure.Core;

namespace AuthApi.Services.Interfaces
{
    public interface IAuthService
    {
        Task<TokenDto> AuthenticateAsync(LoginDto dto, string ipAddress);
        Task RegisterAsync(UserDto dto);
        Task<TokenDto> RefreshTokenAsync(string token, string ipAddress);
    }
}
