using AuthApi.Exceptions;
using AuthApi.Helpers;
using AuthApi.Models;
using AuthApi.Repositories.Interfaces;
using AuthApi.Services.Interfaces;

namespace AuthApi.Services.Implementations
{
    public class AuthService(
        IUserRepository userRepository,
        ITokenHelper tokenHelper,
        IRefreshTokenRepository refreshRepo)
        : IAuthService
    {
        public async Task<TokenDto> AuthenticateAsync(LoginDto dto, string ipAddress)
        {
            User? user = await userRepository.GetByUsernameAsync(dto.Username);
            if (user == null || !PasswordHelper.VerifyPasswordHash(dto.Password, user.PasswordHash, user.PasswordSalt))
                throw new InvalidCredentialsException();

            if (user.IsBlocked)
                throw new UserBlockedException();

            RefreshToken refreshToken = tokenHelper.CreateRefreshToken(user.Id, ipAddress);

            await refreshRepo.AddAsync(refreshToken);
            await refreshRepo.SaveChangesAsync();

            TokenDto token = new TokenDto()
            {
                AccessToken = tokenHelper.CreateToken(user),
                RefreshToken = refreshToken.Token
            };

            return token;
        }

        public async Task RegisterAsync(UserDto dto)
        {
            User? current = userRepository.GetByUsernameAsync(dto.Username).Result;
            if (current != null)
                throw new UsernameInUseException();

            PasswordHelper.CreatePasswordHash(dto.Password, out var hash, out var salt);
            var user = new User
            {
                Username = dto.Username,
                PasswordHash = hash,
                PasswordSalt = salt
            };

            await userRepository.AddAsync(user);
            await userRepository.SaveChangesAsync();
        }

        public async Task<TokenDto> RefreshTokenAsync(string token, string ipAddress)
        {
            RefreshToken? existingToken = await refreshRepo.GetByTokenAsync(token);
            if (existingToken == null || !existingToken.IsActive)
                throw new RefreshTokenExpiredException();

            existingToken.Revoked = DateTime.UtcNow;
            existingToken.RevokedByIp = ipAddress;

            RefreshToken newRefreshToken = tokenHelper.CreateRefreshToken(existingToken.User.Id, ipAddress);
            await refreshRepo.AddAsync(newRefreshToken);

            await refreshRepo.SaveChangesAsync();

            string newAccessToken = tokenHelper.CreateToken(existingToken.User);

            return new TokenDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken.Token
            };
        }
    }
}
