namespace AuthApi.Models
{
    public class TokenDto
    {
        public TokenDto()
        {
        }

        public TokenDto(string accessToken, string refreshToken)
        {
            AccessToken = accessToken;
            RefreshToken = refreshToken;
        }

        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
    }
}
