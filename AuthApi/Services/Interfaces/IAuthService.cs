using AuthApi.Models;
using Azure.Core;

namespace AuthApi.Services.Interfaces
{
    public interface IAuthService
    {
        Task<TokenDto> AuthenticateAsync(LoginRequest dto, string ipAddress);
        Task RegisterAsync(UserRequest dto);
        Task<TokenDto> RefreshTokenAsync(string token, string ipAddress);
    }
}
