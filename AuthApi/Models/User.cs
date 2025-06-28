namespace AuthApi.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public byte[] PasswordHash { get; set; } = null!;
        public byte[] PasswordSalt { get; set; } = null!;
        public bool IsBlocked { get; set; } = false;
        public string Role { get; set; } = "User";
        public List<RefreshToken> RefreshTokens { get; set; } = new();
    }
}
