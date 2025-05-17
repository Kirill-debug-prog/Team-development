using System.ComponentModel.DataAnnotations;

namespace ConsultantPlatform.Models.DTO
{
    public class LoginDTO
    {
        [Required(ErrorMessage = "Login is required")]
        public string Login { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 100 characters")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

    }
}
