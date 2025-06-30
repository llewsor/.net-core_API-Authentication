using System.ComponentModel.DataAnnotations;

namespace AuthApi.Models
{
    public class UserDto
    {
        private readonly string _username = string.Empty;

        [Required]
        public string Username
        {
            get => _username;
            init => _username = value.ToLowerInvariant();
        }
        
        [Required]
        public string Password { get; set; } = null!;
    }
}
