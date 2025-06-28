using System.ComponentModel.DataAnnotations;

namespace AuthApi.Models
{
    public class UserDto
    {
        [Required]
        public string Username { get; set; } = null!;
        [Required]
        public string Password { get; set; } = null!;
    }
}
