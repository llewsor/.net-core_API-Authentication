using System.ComponentModel.DataAnnotations;

namespace AuthApi.Models
{
    public class UserRequest
    {
        private readonly string _username = string.Empty;

        [Required]
        [MinLength(3)]
        [MaxLength(20)]
        public string Username
        {
            get => _username;
            init => _username = value.ToLowerInvariant();
        }

        [Required]
        [MinLength(3)]
        [MaxLength(20)]
        public string Password { get; set; } = null!;
    }
}
