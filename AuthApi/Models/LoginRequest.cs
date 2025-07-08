using System.ComponentModel.DataAnnotations;

namespace AuthApi.Models
{
    public class LoginRequest
    {
        [Required]
        [MinLength(3)]
        [MaxLength(20)]
        public string Username { get; set; } = null!;
        [Required]
        [MinLength(3)]
        [MaxLength(20)]
        public string Password { get; set; } = null!;
    }
}
