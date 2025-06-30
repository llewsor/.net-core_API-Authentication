
using AuthApi.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace AuthApi.Helpers
{
    public class TokenHelper(IConfiguration configuration) : ITokenHelper
    {
        public string CreateToken(User user)
        {
            var claims = new[] {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role)
                };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);
            
            var token = new JwtSecurityToken(
                issuer: configuration["Jwt:Issuer"],
                audience: configuration["Jwt:Audience"],
                claims: claims,
                notBefore: DateTime.UtcNow,
                expires: DateTime.UtcNow.AddMinutes(
                    int.Parse(configuration["Jwt:AccessTokenExpirationMinutes"]!)),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public RefreshToken CreateRefreshToken(int userId, string ip)
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);

            return new RefreshToken
            {
                Token = Convert.ToBase64String(randomBytes),
                Expires = DateTime.UtcNow.AddDays(
                    int.Parse(configuration["Jwt:RefreshTokenExpirationDays"]!)),
                Created = DateTime.UtcNow,
                UserId = userId
            };
        }
    }
}
