using AuthApi.Exceptions;
using AuthApi.Helpers;
using AuthApi.Models;
using AuthApi.Repositories.Interfaces;
using AuthApi.Services.Interfaces;

namespace AuthApi.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshRepo;
        private readonly ITokenHelper _tokenHelper;

        public AuthService(IUserRepository userRepository, ITokenHelper tokenHelper, IRefreshTokenRepository refreshRepo)
        {
            _userRepository = userRepository;
            _tokenHelper = tokenHelper;
            _refreshRepo = refreshRepo;
        }

        public async Task<TokenDto> AuthenticateAsync(LoginDto dto, string ipAddress)
        {
            User? user = await _userRepository.GetByUsernameAsync(dto.Username);
            if (user == null || !PasswordHelper.VerifyPasswordHash(dto.Password, user.PasswordHash, user.PasswordSalt))
                throw new InvalidCredentialsException();

            if (user.IsBlocked)
                throw new UserBlockedException();

            var refreshToken = _tokenHelper.CreateRefreshToken(user.Id, ipAddress);

            await _refreshRepo.AddAsync(refreshToken);
            await _refreshRepo.SaveChangesAsync();

            TokenDto token = new TokenDto()
            {
                AccessToken = _tokenHelper.GenerateToken(user),
                RefreshToken = refreshToken.Token
            };

            return token;
        }

        public async Task RegisterAsync(UserDto dto)
        {
            PasswordHelper.CreatePasswordHash(dto.Password, out var hash, out var salt);
            var user = new User
            {
                Username = dto.Username,
                PasswordHash = hash,
                PasswordSalt = salt
            };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();   
        }

        public async Task<TokenDto> RefreshTokenAsync(string token, string ipAddress)
        {
            RefreshToken? existingToken = await _refreshRepo.GetByTokenAsync(token);
            if (existingToken == null || !existingToken.IsActive)
                throw new RefreshTokenExpiredException();

            existingToken.Revoked = DateTime.UtcNow;
            existingToken.RevokedByIp = ipAddress;

            RefreshToken newRefreshToken = _tokenHelper.CreateRefreshToken(existingToken.User.Id, ipAddress);
            await _refreshRepo.AddAsync(newRefreshToken);

            await _refreshRepo.SaveChangesAsync();

            string newAccessToken = _tokenHelper.GenerateToken(existingToken.User);

            return new TokenDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken.Token
            };
        }
    }
}
