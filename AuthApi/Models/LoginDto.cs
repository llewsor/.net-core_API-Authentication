using System.ComponentModel.DataAnnotations;

namespace AuthApi.Models
{
    public class LoginDto
    {
        [Required]
        public string Username { get; set; } = null!;
        [Required]
        public string Password { get; set; } = null!;
    }
}
